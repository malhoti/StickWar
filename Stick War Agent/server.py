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
from elo import EloManager


input_dim = 15     # 15 input states, like gold, unit count etc
output_dim = 7      # Number of actions


model_save_frequency = 5
render_episode_frequency = 10
MAX_STEPS_PER_EPISODE = 1500
EPOCHS = 1000
MID_UPDATE_INTERVAL = 100
dqn_batch_size = 128
ELO_ENABLED = True   #  keep this true for elo evaluation, this is done at the end 

if ELO_ENABLED:
    EPOCHS = 7 # if we are evaluations two models then run only 100 games max

# what model you want to load for ppo
load_ppo_model_path = "models/PPO_agent_trained_v3.pth"
# what you want the model to save as during training 
save_ppo_model_path = "models/PPO_agent_v4.pth"
# what to save model if winrate threshold is reached
save_ppo_trained_model_path = "models/PPO_agent_trained_v4.pth"

load_dqn_model_path = "models/DQN_agent_trained_v2.pth"
save_dqn_model_path = "models/DQN_agent_v3.pth"
save_dqn_trained_model_path = "models/DQN_agent_trained_v3.pth"

# Server configuration
HOST = '127.0.0.1'
PORT = 5000

# Global dictionaries for agents and metrics
agents = {}            # { agent_id: PPOAgent instance }
all_metrics = {}       # { agent_id: metrics dict }
csv_loggers = {}       # { agent_id: CSVLogger instance }

elo_mgr = EloManager(path="elos.json", k=32, enabled=ELO_ENABLED)

def extract_state(state_dict):
    """
    Convert state dictionary to a numpy array.
    """

    state_list = [
        state_dict['gold'],
        state_dict['health'],
        state_dict['miners'],
        state_dict['swordsmen'],
        state_dict['archers']
    ]
    # flatten stateValue
    state_list.extend(state_dict['stateValue'])
    
    # Add the remaining values
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
    Loads an existing model if available, and also initialises the CSV logger and metrics.


    """
    # hyperparemters
    gamma = 0.99        # discount factor for future rewards
    ppo_clip = 0.15
    update_epochs = 4
    learning_rate = 0.0002
    value_coef = 0.5
    entropy_coef = 0.05

    ppo_model = PPO(input_dim, output_dim).to(device)
    optimiser = optim.Adam(ppo_model.parameters(), lr=learning_rate)
    agent_instance = PPOAgent(ppo_model, optimiser, ppo_clip, gamma, update_epochs, value_coef, entropy_coef, device)
    model_path =""
    
    model_path = load_ppo_model_path
    
    
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
    Loads an existing model if available, and also initialises the CSV logger and metrics.
    """
    # hyperparatmers
    gamma = 0.99        # Discount factor for future rewards
    learning_rate = 0.00005
    
    # Here we create the main and target network
    dqn_model = DQN(input_dim, output_dim).to(device)
    target_dqn_model = DQN(input_dim, output_dim).to(device)
    
    optimizer = optim.Adam(dqn_model.parameters(), lr=learning_rate)
    
    # Initialise the replay memory
    replay_memory = ReplayMemory(capacity=10000)  
    
    agent_instance = DQNAgent(dqn_model, target_dqn_model, optimizer, replay_memory, device, output_dim, gamma)
    
    model_path = load_dqn_model_path
    
    
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

    # Buffer for accumulating data (using \n as a delimiter)
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

                if message.get("type") == "agent_interaction":
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

                                ## elo rating
                                if agent_id == "1": # e.g. only update for your primary matchup
                                    
                                    
                                    current_model_type = data_dict["model_type"].upper()
                                    ## the oppoent model could also be the hard coded ai that ive made in unity, however they dont send data to python so it wont exist in the dictionary so 
                                    # you will have to manually change this value to its name, for example hard ai, easy ai. Comment out the appropriate one

                                    #opponent_model_type = agents_data["2"]["model_type"].upper()
                                    opponent_model_type = "DQN"

                                    # map your int result to Elo score for agent1:
                                    if result == 0:
                                        score = 1.0
                                    elif result == 1:
                                        score = 0.0
                                    else:
                                        score = 0.5
                                    
                                    elo_mgr.update(current_model_type, opponent_model_type, score)

                        else:  # training mode
            
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
                                    # update PPO.
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
                                        agent_instance.save_model(agent_id, model_path=save_ppo_model_path)
                                    if isinstance(agent_instance,DQNAgent):
                                        agent_instance.save_model(agent_id, model_path=save_dqn_model_path)
                                
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


                                # calculate winrates over the most rescent 100 games
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


                # this is only called if both the teams in unity, are non rl agents, such as Hard ai, easy ai or human
                # this spereates complexity from the previous one making it easier to understand, doesnt require sending action or anything else back to unity

                elif message.get("type") == "match_result":
                    print(message)
                    a1 = message["agent1"]    # e.g. "HARD_AI" or "EASY_AI"
                    a2 = message["agent2"]
                    res = message["result"]   # 0 = agent1 wins, 1 = agent2 wins, 2 = draw

                    # map to Elo score for a1
                    if res == 0:    score1 = 1.0
                    elif res == 1:  score1 = 0.0
                    else:           score1 = 0.5

                    elo_mgr.update(a1, a2, score1)
                    episode +=1
                    
                    


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
                        agent_instance.save_model(agent_id, model_path=save_ppo_trained_model_path)
                    if isinstance(agent_instance,DQNAgent):
                        agent_instance.save_model(agent_id, model_path=save_dqn_trained_model_path)
                else:
                    if isinstance(agent_instance,PPOAgent):
                        agent_instance.save_model(agent_id, model_path=save_ppo_model_path)
                    if isinstance(agent_instance,DQNAgent):
                        agent_instance.save_model(agent_id, model_path=save_dqn_model_path)
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
    # live plotting for metrics (if desired).
    live_plot_all_metrics(all_metrics)
