import json
import os
import socket
import threading
import torch
import torch.optim as optim
import numpy as np
import queue

from PPO import PPO
from PPOAgent import PPOAgent
from DQN import DQN, ReplayMemory
from DQNAgent import DQNAgent
from CSVLogger import CSVLogger
from Visualise import live_plot_all_metrics

# Hyperparameters
input_dim = 15     # Adjust based on your state vector
output_dim = 7      # Number of actions


model_save_frequency = 5
render_episode_frequency = 10
MAX_STEPS_PER_EPISODE = 1500
EPOCHS = 1000
MID_UPDATE_INTERVAL = 100
dqn_batch_size = 128


# Server configuration
HOST = '127.0.0.1'
PORT = 5000

# Global dictionaries for agents and metrics
agents = {}            # { agent_id: PPOAgent instance }
all_metrics = {}       # { agent_id: metrics dict }

csv_loggers = {}       # { agent_id: CSVLogger instance }

def extract_state(state_dict):
    """
    Convert state dictionary to a numpy array.
    Adjust the ordering based on your environment.
    """

    state_list = [
        state_dict['gold'],
        state_dict['health'],
        state_dict['miners'],
        state_dict['swordsmen'],
        state_dict['archers']
    ]
    # Extend the list with the one-hot encoded values (flatten stateValue)
    state_list.extend(state_dict['stateValue'])
    
    # Add the remaining scalar values
    state_list.extend([
        state_dict['nearby_resources_available'],
        state_dict['enemy_health'],
        state_dict['enemy_miners'],
        state_dict['enemy_swordsmen'],
        state_dict['enemy_archers'],
        state_dict['enemies_in_vicinity'],
        state_dict['episode_time']
    ])
    return np.array(state_list,dtype=np.float32)

def initialise_ppo_agent(agent_id,agent_mode, device):
    """
    Create a new PPO model and PPOAgent instance for a given agent_id.
    Loads an existing model if available, and also initializes the CSV logger and metrics.


    """
    gamma = 0.99        # Discount factor for future rewards
    ppo_clip = 0.15
    update_epochs = 4
    learning_rate = 0.0002
    value_coef = 0.5
    entropy_coef = 0.05

    ppo_model = PPO(input_dim, output_dim).to(device)
    optimiser = optim.Adam(ppo_model.parameters(), lr=learning_rate)
    agent_instance = PPOAgent(ppo_model, optimiser, ppo_clip, gamma, update_epochs, value_coef, entropy_coef, device)
    model_path =""

    # we load models that are in eval mode, not training, 
    # if we for some reason have to load up a model in training, i will manually allow that, 
    
    model_path = "models/PPO_agent_trained_v3.pth"
    
    
    agent_instance.load_model(agent_id, model_path)
    

    # Initialize metrics and CSV logger for this agent.
    all_metrics[agent_id] = {
        "mode":"",
        "episode_rewards": [],
        "episodes": [],
        "result" :[],
        "policy_losses": [],
        "value_losses": [],
        "total_losses": [],
        "entropies": [],
        "action_counts": {0: 0, 1: 0, 2: 0, 3: 0, 4: 0, 5: 0, 6: 0}
    }
    csv_loggers[agent_id] = CSVLogger(f"{agent_id}_states_logger.csv")
    return agent_instance

def initialise_dqn_agent(agent_id,agent_mode, device):
    """
    Create a new PPO model and PPOAgent instance for a given agent_id.
    Loads an existing model if available, and also initializes the CSV logger and metrics.
    """

    gamma = 0.99        # Discount factor for future rewards
    learning_rate = 0.00005
    
    dqn_model = DQN(input_dim, output_dim).to(device)
    target_dqn_model = DQN(input_dim, output_dim).to(device)
    
    # Create the optimizer for the primary network.
    optimizer = optim.Adam(dqn_model.parameters(), lr=learning_rate)
    
    # Initialize the replay memory; ensure you have an appropriate ReplayMemory class available.
    replay_memory = ReplayMemory(capacity=10000)  # example capacity value
    
    # Now, create the DQNAgent with only the parameters it requires.
    # The DQNAgent initializer is assumed to be:
    # def __init__(self, dqn_model, target_dqn_model, optimizer, replay_memory, device, output_dim, gamma=0.99)
    agent_instance = DQNAgent(dqn_model, target_dqn_model, optimizer, replay_memory, device, output_dim, gamma)
    model_path =""
    # we load models that are in eval mode, not training, 
    # if we for some reason have to load up a model in training, i will manually allow that, 
    
    model_path = "models/DQN_agent_v3.pth"
    
    
    agent_instance.load_model(agent_id, model_path)
    

    # Initialize metrics and CSV logger for this agent.
    all_metrics[agent_id] = {
        "mode":"",
        "episode_rewards": [],
        "episodes": [],
        "result" :[],
        "policy_losses": [],
        "value_losses": [],
        "total_losses": [],
        "entropies": [],
        "action_counts": {0: 0, 1: 0, 2: 0, 3: 0, 4: 0, 5: 0, 6: 0}
    }
    csv_loggers[agent_id] = CSVLogger(f"{agent_id}_states_logger.csv")
    return agent_instance



def handle_client(conn, addr):
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    print(f"Connected by {addr}")

    # Buffer for accumulating data (using newline as a delimiter)
    buffer = ""

    # We assume Unity sends messages of type "state" with an "agents" dict.
    step = 0
    episode = 1
    total_reward = {}
    increment_episode = False
    win_rate_achieved = False

    # This dictionary will store previous transition information for DQN agents.
    # For each DQN agent we store: (prev_state, prev_action, prev_reward, prev_done)
    dqn_prev_transition = {}

    try:
        while episode <= EPOCHS and not win_rate_achieved:
            data = conn.recv(4096)
            if not data:
                print("Connection closed by client.")
                break

            buffer += data.decode('utf-8')
            # Process complete messages (each ending with "\n")
            while "\n" in buffer:
                line, buffer = buffer.split("\n", 1)
                if not line.strip():
                    continue
                try:
                    message = json.loads(line)
                except json.JSONDecodeError as e:
                    print("JSON decode error:", e)
                    continue

                if message.get("type") == "state":
                    agents_data = message.get("agents", {})
                    response_actions = {}

                    # Process each agent from the aggregated message.
                    for agent_id, data_dict in agents_data.items():
                        agent_mode = data_dict.get("agent_type", "training")

                        model_type = data_dict.get("model_type", "ppo")
                        # If this is a new agent, initialize it.
                        if agent_id not in agents:
                            if model_type == "ppo":
                                agents[agent_id] = initialise_ppo_agent(agent_id, agent_mode, device)
                            if model_type == "dqn":
                                agents[agent_id] = initialise_dqn_agent(agent_id, agent_mode,device)
                            total_reward[agent_id] = 0

                        agent_instance = agents[agent_id]

                        # Set mode: eval or training.
                        if agent_mode == "eval":
                            agent_instance.model.eval()
                        else:
                            agent_instance.model.train()

                        # Extract the state, reward, and done flag.
                        state_dict = data_dict.get("state")
                        current_state = extract_state(state_dict)
                        current_reward = data_dict.get("reward", 0)
                        done = data_dict.get("done", False)
                        result = data_dict.get("result", False)
                        max_steps_reached = (step >= MAX_STEPS_PER_EPISODE)

                        # Update total reward.
                        total_reward[agent_id] += current_reward

                        # Get the CSV logger for this agent.
                        logger = csv_loggers[agent_id]

                        if agent_mode == "eval":
                            # Evaluation mode: select action only (dummy extras can be ignored).
                            action, _, _ = agent_instance.select_action(current_state)
                            all_metrics[agent_id]["action_counts"][action] += 1

                            if not done:
                                response_actions[agent_id] = {
                                    "action": action,
                                    "render": (episode % render_episode_frequency == 0),
                                    "maxStepsReached": max_steps_reached,
                                    "episode": episode,
                                    "step": step,
                                    "eval": True
                                }
                            else:
                                # End of episode logging for evaluation.
                                all_metrics[agent_id]["mode"] = "eval"
                                all_metrics[agent_id]["episode_rewards"].append(total_reward[agent_id])
                                all_metrics[agent_id]["episodes"].append(episode)
                                response_actions[agent_id] = {"reset_ack": True}
                                print(f"Agent {agent_id} : Episode {episode}, Total Reward: {total_reward[agent_id]}")
                                total_reward[agent_id] = 0
                                increment_episode = True
                                step = 0

                        else:  # training mode
                            # Unified action selection.
                            action, log_prob, value = agent_instance.select_action(current_state)
                            
                            # Store transition differently based on agent type.
                            if isinstance(agent_instance, PPOAgent):
                                # PPO expects a 6-tuple: (state, action, reward, log_prob, value, done)
                                agent_instance.store_transition((current_state, action, current_reward, log_prob, value, done))

                            elif isinstance(agent_instance, DQNAgent):
                                # DQN expects a 5-tuple: (state, action, reward, next_state, done)
                                # Since Unity sends the "current_state" in each message, we need to use the previous state.
                                if agent_id not in dqn_prev_transition:
                                    # First data point for this agent—store current state and delay transition storage.
                                    dqn_prev_transition[agent_id] = (current_state, action, current_reward, done)
                                else:
                                    # Retrieve the previous data.
                                    prev_state, prev_action, prev_reward, prev_done = dqn_prev_transition[agent_id]
                                    # Now, current_state is the next state relative to the previous data.
                                    dqn_transition = (prev_state, prev_action, prev_reward, current_state, prev_done)
                                    
                                    agent_instance.store_transition(dqn_transition)
                                    # Update the dictionary with the current state's info for the next transition.
                                    dqn_prev_transition[agent_id] = (current_state, action, current_reward, done)

                            # Update action counts and log the state.
                            if action not in all_metrics[agent_id]["action_counts"]:
                                print(f"Unexpected action: {action}. Available keys: {all_metrics[agent_id]['action_counts'].keys()}")
                            else:
                                all_metrics[agent_id]["action_counts"][action] += 1

                            logger.log_state(step, list(current_state) + [action])

                            if not done:
                                response_actions[agent_id] = {
                                    "action": action,
                                    "render": (episode % render_episode_frequency == 0),
                                    "maxStepsReached": max_steps_reached,
                                    "episode": episode,
                                    "step": step
                                }

                                # For PPO, perform a mid-episode update if appropriate.
                                if isinstance(agent_instance, PPOAgent) and step > 0 and step % MID_UPDATE_INTERVAL == 0:
                                    state_tensor = torch.tensor(current_state, dtype=torch.float32).unsqueeze(0).to(device)
                                    with torch.no_grad():
                                        _, bootstrap_value = agent_instance.model(state_tensor)
                                    returns, advantages = agent_instance.compute_returns_and_advantages(bootstrap_value, done=False)
                                    agent_instance.update(returns, advantages)
                                
                                elif isinstance(agent_instance, DQNAgent) and step > 0 and step % MID_UPDATE_INTERVAL == 0:
                                # For DQN, call update with a specified batch size if the replay memory is sufficiently populated.
                                
                                    if len(agent_instance.replay_memory) >= dqn_batch_size:
                                        agent_instance.update(dqn_batch_size)
                                    
                            else:
                                # Terminal condition: end of episode.
                                if isinstance(agent_instance, PPOAgent):
                                    # Compute bootstrap value and update PPO.
                                    state_tensor = torch.tensor(current_state, dtype=torch.float32).unsqueeze(0).to(device)
                                    with torch.no_grad():
                                        _, last_value = agent_instance.model(state_tensor)
                                    if len(agent_instance.memory) > 0:
                                        returns, advantages = agent_instance.compute_returns_and_advantages(last_value, done)
                                        agent_instance.update(returns, advantages)
                                    else:
                                        print("No transitions stored in final update, skipping.")
                                
                                elif isinstance(agent_instance, DQNAgent):
                                    # For DQN, if we have a stored previous transition, finalize it with the current state.
                                    if agent_id in dqn_prev_transition:
                                        prev_state, prev_action, prev_reward, prev_done = dqn_prev_transition[agent_id]
                                        dqn_transition = (prev_state, prev_action, prev_reward, current_state, prev_done)
                                        agent_instance.store_transition(dqn_transition)
                                        if len(agent_instance.replay_memory) >= dqn_batch_size:
                                            agent_instance.update(dqn_batch_size)
                                        del dqn_prev_transition[agent_id]
                                
                                print(f"Agent {agent_id} : Episode {episode}, Total Reward: {total_reward[agent_id]}")
                                
                                # Save model periodically.
                                if episode % model_save_frequency == 0:
                                    if isinstance(agent_instance,PPOAgent):
                                        agent_instance.save_model(agent_id, model_path="models/PPO_agent_v4.pth")
                                    if isinstance(agent_instance,DQNAgent):
                                        agent_instance.save_model(agent_id, model_path="models/DQN_agent_v3.pth")
                                
                                # Log metrics.
                                all_metrics[agent_id]["mode"] = "train"
                                all_metrics[agent_id]["episode_rewards"].append(total_reward[agent_id])
                                all_metrics[agent_id]["result"].append(result)
                                all_metrics[agent_id]["episodes"].append(episode)
                                all_metrics[agent_id]["total_losses"].append(agent_instance.total_loss)
                                if isinstance(agent_instance, PPOAgent):
                                    all_metrics[agent_id]["policy_losses"].append(agent_instance.policy_loss)
                                    all_metrics[agent_id]["value_losses"].append(agent_instance.value_loss)
                                    
                                    all_metrics[agent_id]["entropies"].append(agent_instance.entropy)
                                
                                total_reward[agent_id] = 0
                                response_actions[agent_id] = {"reset_ack": True}
                                increment_episode = True
                                step = 0

                                last_results = all_metrics[agent_id]["result"][-100:] if len(all_metrics[agent_id]["result"]) >= 100 else all_metrics[agent_id]["result"]

                                

                                wins = sum(1 for r in last_results if r == 0)
                                
                                total_games = len(last_results)

                                win_rate = wins / total_games

                                if win_rate >= 0.65 and total_games >= 100:
                                    win_rate_achieved = True

                                

                    # End-of-episode logic
                    if increment_episode:
                        episode += 1
                        increment_episode = False    

                    step += 1
                    # Send back aggregated response.
                    response = {"type": "action", "agents": response_actions}
                    response_str = json.dumps(response) + "\n"
                    conn.sendall(response_str.encode('utf-8'))

                else:
                    print("Received unknown message type:", message.get("type"))
                    print("This is the message: ", message)
    except Exception as e:
        print(f"Error handling client {addr}: {e}")
    finally:
        # Save final models for all agents.
        for agent_id, agent_instance in agents.items():
            if agent_id == "1": # since only team 1 is training and team 2 will be in eval mode theres no need to save
                if win_rate_achieved:
                    if isinstance(agent_instance,PPOAgent):
                        agent_instance.save_model(agent_id, model_path="models/PPO_agent_trained_v4.pth")
                    if isinstance(agent_instance,DQNAgent):
                        agent_instance.save_model(agent_id, model_path="models/DQN_agent_trained_v3.pth")
                else:
                    if isinstance(agent_instance,PPOAgent):
                        agent_instance.save_model(agent_id, model_path="models/PPO_agent_v4.pth")
                    if isinstance(agent_instance,DQNAgent):
                        agent_instance.save_model(agent_id, model_path="models/DQN_agent_v3.pth")
                print(f"Final model saved for agent {agent_id}")
        conn.close()
        print(f"Connection with {addr} closed.")


def server_main():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
        server_socket.bind((HOST, PORT))
        server_socket.listen()
        print("Server is listening on {}:{}".format(HOST, PORT))
        while True:
            conn, addr = server_socket.accept()
            client_thread = threading.Thread(target=handle_client, args=(conn, addr))
            client_thread.start()

if __name__ == "__main__":
    server_thread = threading.Thread(target=server_main, daemon=False)
    server_thread.start()
    # Launch live plotting for metrics (if desired).
    live_plot_all_metrics(all_metrics)
