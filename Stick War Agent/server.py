import json
import os
import socket
import threading
import torch
import torch.optim as optim
import torch.nn.functional as F
import random
import numpy as np
import queue
from DQN import DQN
from replayMemory import ReplayMemory
from DQN_utils import DQNAgent

from PPO import PPO
from PPOAgent import PPOAgent
from Visualise import plot_all_metrics ,live_plot_all_metrics
from CSVLogger import CSVLogger

# Hyperparameters
input_dim = 13      # Example input dimension, adjust based on your state vector
output_dim = 7      # Example output dimension (number of actions)
gamma = 0.99        # Discount factor for future rewards
ppo_clip = 0.2
update_epochs = 4
learning_rate = 0.001
value_coef = 0.5
entropy_coef = 0.5

# batch_size = 64
# epsilon = 1.0       # Initial epsilon for exploration
# epsilon_min = 0.1
# epsilon_decay = 0.995
# target_update_frequency = 1000  # Update target network every 1000 steps
# batch_size = 64                 # for buffer memory
# buffer_capacity = 10000
# learning_rate = 0.001

model_save_frequency = 100
render_episode_frequency = 10
plot_update_frequency = 100
MAX_STEPS_PER_EPISODE = 500
MAX_EPISODE = 100

#agent = DQNAgent(dqn_model, target_dqn_model, optimizer, replay_memory, device, output_dim, gamma)
# Server configuration
HOST = '127.0.0.1'  # Localhost
PORT = 5000         # Port to bind the server to

reward_queue = queue.Queue()


all_metrics = {}



agent_id = 1
episode_rewards = []        # List of total rewards per episode


policy_losses = []            # List of policy losses per episode (or per update)
value_losses = []              # List of value losses per episode
total_losses = []             # List of total losses per episode
entropies = []                # List of entropy values per episode
action_counts = {0: 0, 1: 0, 2: 0, 3: 0, 4: 0, 5: 0, 6: 0} # keeps count of each action that was played out in that episode
  



def handle_client(conn, addr):
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    ppo_model = PPO(input_dim, output_dim).to(device)
    optimiser = optim.Adam(ppo_model.parameters(), lr=learning_rate)
    agent = PPOAgent(ppo_model, optimiser, ppo_clip, gamma, update_epochs, value_coef, entropy_coef, device)

    print(f"Connected by {addr}")

    
    # Episode and transition tracking variables
    episode = 1
    total_reward = 0
    step = 0
    max_steps_reached = False

    prev_state = None
    last_action = None
    last_log_prob = None
    last_value = None
    last_reward = 0  # reward from previous cycle

    try:
        # Wait for the initial message from Unity
        data = conn.recv(1024)
        if data:
            data_dict = json.loads(data.decode())
            agent_id = data_dict.get('agent_id')
            # Load model if it exists...
            model_path = f"models/model_agent_{agent_id}.pth"
            if os.path.exists(model_path):
                agent.load_model(agent_id)
                print(f"Loaded model for agent {agent_id}")
            else:
                print(f"No saved model found for agent {agent_id}, starting with a new model.")


            all_metrics[agent_id] = {  
                "episode_rewards": [],
                "episodes": [],
                "policy_losses": [],
                "value_losses": [],
                "total_losses": [],
                "entropies": [],
                "action_counts": {0:0 , 1: 0, 2: 0, 3: 0, 4: 0, 5: 0, 6: 0}
            }


            csv_logger  = CSVLogger(f"{agent_id}states_logger.csv")
            # Process the initial state from Unity
            state_dict = data_dict.get('state')
            done = data_dict.get('done')
            current_state = np.array([
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
            # Optionally initialize total_reward if provided
            current_reward = data_dict.get('reward', 0)


        # Main loop: each iteration handles one message from Unity and responds with an action.
        while episode <= MAX_EPISODE:

            if step >= MAX_STEPS_PER_EPISODE:
                max_steps_reached = True
            if not (done):
                # Normal case: select an action and send the action message.
                action, log_prob, value = agent.select_action(current_state)
                last_action = action
                last_log_prob = log_prob
                last_value = value
                last_reward = current_reward  # save current reward for transition
                

                # update the action frequency counter
                all_metrics[agent_id]["action_counts"][action] += 1
                
                # Log state and action.
                csv_logger.log_state(step, list(current_state) + [action])
                render_episode = (episode % render_episode_frequency == 0)

                response = json.dumps({
                    "agent_id": agent_id,
                    "action": action,
                    "render": render_episode,
                    "maxStepsReached": max_steps_reached,
                    "episode": episode,
                    "step": step
                })
                conn.sendall(response.encode())
                
            else:
                # Terminal case: do not send normal action message.
                print(f"Episode {episode} terminal condition reached (done: {done}, max_steps: {max_steps_reached}).")
            
            

            # Receive the current state (and reward/done) from Unity
            data = conn.recv(1024)
            if not data:
                print(agent_id, "heloo")
                break

            data_dict = json.loads(data.decode())
            agent_id = data_dict.get('agent_id')
            state_dict = data_dict.get('state')
            current_reward = data_dict.get('reward')
            done = data_dict.get('done')
            # Convert current state
            new_state = np.array([
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
           
            # Update cumulative reward and step count
            total_reward += current_reward

            if step % plot_update_frequency == 0:
                reward_queue.put((agent_id,total_reward))
                #print(f"Pushed reward for agent {agent_id}: {total_reward}")

            
            
            
            # Build the transition from the previous state (current_state) to new_state.
            # The reward and terminal flag from the last cycle are now associated with that transition.
            
            if prev_state is None:
                # For the very first iteration, we don't have a complete transition yet.
                # So we set prev_state to the current state.
                prev_state = current_state
            else:
                agent.store_transition((prev_state, last_action, last_reward, last_log_prob, last_value, done))

            
              # Save the reward for this cycle (to be used in next transition)
            last_reward = current_reward

            # For the next transition, the current state becomes the previous state.
            prev_state = current_state
            current_state = new_state
            step += 1
            # If the episode is terminal, update the agent and reset counters.
            if (done):
                # Compute bootstrap value for the terminal state
                state_tensor = torch.tensor(current_state, dtype=torch.float32).unsqueeze(0).to(device)
                with torch.no_grad():
                    _, last_value = ppo_model(state_tensor)
                returns, advantages = agent.compute_returns_and_advantages(last_value, done)
                agent.update(returns, advantages)



                
                

                print(f"Agent {agent_id} : Episode {episode}, Total Reward: {total_reward}")

                # Reset episode-specific variables
                total_reward = 0
                episode += 1
                step = 0
                max_steps_reached = False
                prev_state = None  # Start fresh
                last_action = None
                last_log_prob = None
                last_value = None
                last_reward = 0
                # Optionally save model periodically:
                if episode % model_save_frequency == 0:
                    agent.save_model(agent_id)


                reset_ack = json.dumps({"reset_ack": True})
                conn.sendall(reset_ack.encode())
                print(f"Agent {agent_id} : Sent reset ack. Waiting for new initial state from Unity...")


                # After terminal, expect Unity to send a new initial state.
                data = conn.recv(1024)
                if not data:
                    break
                print(f"Agent {agent_id} : Recieved state of new environment")
                data_dict = json.loads(data.decode())
                state_dict = data_dict.get('state')
                current_reward = data_dict.get('reward')
                done  = data_dict.get('done')
                current_state = np.array([
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
                
                

                # ############### GRAPHING ###################
                all_metrics[agent_id]["episode_rewards"].append(total_reward)
                all_metrics[agent_id]["episodes"].append(episode)
                all_metrics[agent_id]["policy_losses"].append(agent.policy_loss)
                all_metrics[agent_id]["value_losses"].append(agent.value_loss)
                all_metrics[agent_id]["total_losses"].append(agent.total_loss)
                all_metrics[agent_id]["entropies"].append(agent.entropy)

                # episode_rewards.append(total_reward)
                # policy_losses.append(agent.policy_loss)
                # value_losses.append(agent.value_loss)
                # total_losses.append(agent.total_loss)
                # entropies.append(agent.entropy)
                

                        # List of total rewards per episode

                
            else:
                prev_state = current_state
            

    except Exception as e:
        print(f"Error handling client {addr}: {e}")
    finally:
        if 'agent_id' in locals():
            agent.save_model(agent_id)
            print(f"Final model saved for agent {agent_id}")
        print(f"Connection with {addr} closed.")
        conn.close()

# Main server function
def server_main():
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
        server_socket.bind((HOST, PORT))
        server_socket.listen()
        print("Server is listening...")

        while True:
            conn, addr = server_socket.accept()  # Wait for a client connection
            # Start a new thread for each connected client
            client_thread = threading.Thread(target=handle_client, args=(conn, addr))
            client_thread.start()
            


if __name__ == "__main__":
    server_thread = threading.Thread(target=server_main, daemon=False)
    server_thread.start()
    #live_plot(reward_queue,plot_update_frequency)
    
    live_plot_all_metrics(all_metrics)
    
