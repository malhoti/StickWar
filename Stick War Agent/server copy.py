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
from CSVLogger import CSVLogger
from Visualise import live_plot_all_metrics

# Hyperparameters
input_dim = 13      # Adjust based on your state vector
output_dim = 7      # Number of actions
gamma = 0.99        # Discount factor for future rewards
ppo_clip = 0.2
update_epochs = 4
learning_rate = 0.001
value_coef = 0.5
entropy_coef = 0.5

model_save_frequency = 1
render_episode_frequency = 10
plot_update_frequency = 100
MAX_STEPS_PER_EPISODE = 500
MAX_EPISODE = 100

# Server configuration
HOST = '127.0.0.1'
PORT = 5000

# Global dictionaries for agents and metrics
agents = {}            # { agent_id: PPOAgent instance }
all_metrics = {}       # { agent_id: metrics dict }
reward_queue = queue.Queue()  # For plotting if needed
csv_loggers = {}       # { agent_id: CSVLogger instance }

def extract_state(state_dict):
    """
    Convert state dictionary to a numpy array.
    Adjust the ordering based on your environment.
    """
    return np.array([
        state_dict['gold'],
        state_dict['health'],
        state_dict['miners'],
        state_dict['swordsmen'],
        state_dict['archers'],
        state_dict['stateValue'],
        state_dict['nearby_resources_available'],
        state_dict['enemy_health'],
        state_dict['enemy_miners'],
        state_dict['enemy_swordsmen'],
        state_dict['enemy_archers'],
        state_dict['enemies_in_vicinity'],
        state_dict['episode_time']
    ], dtype=np.float32)

def initialise_agent(agent_id, device):
    """
    Create a new PPO model and PPOAgent instance for a given agent_id.
    Loads an existing model if available, and also initializes the CSV logger and metrics.
    """
    ppo_model = PPO(input_dim, output_dim).to(device)
    optimiser = optim.Adam(ppo_model.parameters(), lr=learning_rate)
    agent_instance = PPOAgent(ppo_model, optimiser, ppo_clip, gamma, update_epochs, value_coef, entropy_coef, device)

    model_path = f"models/model_agent_{agent_id}.pth"
    if os.path.exists(model_path):
        agent_instance.load_model(model_path)
        print(f"Loaded model for agent {agent_id}")
    else:
        print(f"No saved model found for agent {agent_id}, starting with a new model.")

    # Initialize metrics and CSV logger for this agent.
    all_metrics[agent_id] = {
        "episode_rewards": [],
        "episodes": [],
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
    # Each agent's data should include at least:
    #   - state (dictionary)
    #   - reward (float)
    #   - done (boolean)
    #   - (optionally) episode and step counters if needed.

    step = 0
    episode = 1
    try:
        while True:
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
                print(line)
                try:
                    message = json.loads(line)
                except json.JSONDecodeError as e:
                    print("JSON decode error:", e)
                    continue

                if message.get("type") == "state":
                    agents_data = message.get("agents", {})
                    response_actions = {}

                    # Iterate through each agent in the aggregated message.
                    for agent_id, data_dict in agents_data.items():
                        # If this is a new agent, initialize it.
                        if agent_id not in agents:
                            agents[agent_id] = initialise_agent(agent_id, device)

                        agent_instance = agents[agent_id]
                        # Extract the state, reward, and done flag.
                        state_dict = data_dict.get("state")
                        current_state = extract_state(state_dict)
                        current_reward = data_dict.get("reward", 0)
                        done = data_dict.get("done", False)
                        # episode = data_dict.get("episode", 1)
                        # step = data_dict.get("step", 0)
                        max_steps_reached = (step >= MAX_STEPS_PER_EPISODE)

                        # For logging and metrics update:
                        logger = csv_loggers[agent_id]

                        
                        # Process non-terminal transitions
                        if not done:
                            # Select an action using the agent's policy.
                            action, log_prob, value = agent_instance.select_action(current_state)
                            # (Optionally) store the transition. You may want to implement a method
                            # inside PPOAgent to handle transition storage.
                            agent_instance.store_transition((current_state, action, current_reward, log_prob, value, done))
                            # Update action counts, logging, etc.
                            all_metrics[agent_id]["action_counts"][action] += 1
                            logger.log_state(step, list(current_state) + [action])
                            response_actions[agent_id] = {
                                "action": action,
                                "render": (episode % render_episode_frequency == 0),
                                "maxStepsReached": max_steps_reached,
                                "episode": episode,
                                "step": step
                            }
                        else:
                            # Terminal condition: update the agent, save model, and prepare reset.
                            # Compute bootstrap value for the terminal state.
                            state_tensor = torch.tensor(current_state, dtype=torch.float32).unsqueeze(0).to(device)
                            with torch.no_grad():
                                _, last_value = agent_instance.model(state_tensor)
                            returns, advantages = agent_instance.compute_returns_and_advantages(last_value, done)
                            agent_instance.update(returns, advantages)
                            
                            print(f"Agent {agent_id} : Episode {episode}, Total Reward: {current_reward}")
                            
                            # Optionally, save the model periodically.
                            if episode % model_save_frequency == 0:
                                model_path = f"models/model_agent_{agent_id}.pth"
                                agent_instance.save_model(model_path)
                                print(f"Saved model for agent {agent_id} at episode {episode}")

                            # Log metrics for graphing.
                            all_metrics[agent_id]["episode_rewards"].append(current_reward)
                            all_metrics[agent_id]["episodes"].append(episode)
                            all_metrics[agent_id]["policy_losses"].append(agent_instance.policy_loss)
                            all_metrics[agent_id]["value_losses"].append(agent_instance.value_loss)
                            all_metrics[agent_id]["total_losses"].append(agent_instance.total_loss)
                            all_metrics[agent_id]["entropies"].append(agent_instance.entropy)

                            # Respond with a reset acknowledgment.
                            response_actions[agent_id] = {"reset_ack": True}

                            episode += 1
                            step = 0
                            
                    step += 1
                    # Send back an aggregated response for all agents.
                    response = {"type": "action", "agents": response_actions}
                    response_str = json.dumps(response) + "\n"
                    conn.sendall(response_str.encode('utf-8'))

                else:
                    print("Received unknown message type:", message.get("type"))
                    print("This is the message: " ,message)
    except Exception as e:
        print(f"Error handling client {addr}: {e}")
    finally:
        # Save final models for all agents.
        for agent_id, agent_instance in agents.items():
            model_path = f"models/model_agent_{agent_id}.pth"
            agent_instance.save_model(model_path)
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
            # For centralized communication, you may only have one connection
            # coming from Unity. If there are multiple, adjust your design accordingly.
            client_thread = threading.Thread(target=handle_client, args=(conn, addr))
            client_thread.start()

if __name__ == "__main__":
    server_thread = threading.Thread(target=server_main, daemon=False)
    server_thread.start()
    # Launch live plotting for metrics (if desired).
    live_plot_all_metrics(all_metrics)
