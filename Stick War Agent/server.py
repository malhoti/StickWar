import json
import os
import socket
import threading
import torch
import torch.optim as optim
import torch.nn.functional as F
import random
import numpy as np
from DQN import DQN
from replayMemory import ReplayMemory
from DQN_utils import DQNAgent


# Hyperparameters
input_dim = 13 # Example input dimension, adjust based on your state vector
output_dim = 7  # Example output dimension (number of actions)
gamma = 0.99  # Discount factor for future rewards
batch_size = 64
epsilon = 1.0  # Initial epsilon for exploration
epsilon_min = 0.1
epsilon_decay = 0.995
target_update_frequency = 1000  # Update target network every 1000 steps
batch_size = 64 # for buffer memory
buffer_capacity = 10000
learning_rate = 0.001

model_save_frequency = 100
render_episode_frequency = 10


#agent = DQNAgent(dqn_model, target_dqn_model, optimizer, replay_memory, device, output_dim, gamma)
# Server configuration
HOST = '192.168.1.206'  # Localhost
PORT = 5000         # Port to bind the server to



def handle_client(conn, addr):
    dqn_model = DQN(input_dim, output_dim)
    target_dqn_model = DQN(input_dim, output_dim)
    target_dqn_model.load_state_dict(dqn_model.state_dict())  # Copy weights to target model initially
    target_dqn_model.eval()  # Target network is in evaluation mode
    optimizer = optim.Adam(dqn_model.parameters(), lr=learning_rate)
    replay_memory = ReplayMemory(buffer_capacity)
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

    agent = DQNAgent(dqn_model, target_dqn_model, optimizer, replay_memory, device, output_dim, gamma)
    global epsilon
    
    print(f"Connected by {addr}")
    step = 0  # Track steps for target network update
    total_reward = 0
    episode = 0

    try:
        data = conn.recv(1024)
        if data:
            data_dict = json.loads(data.decode())
            agent_id = data_dict.get('agent_id')  # Extract agent_id from the first message
            
            # Load model based on agent_id after receiving the first message
            model_path = f"models/model_agent_{agent_id}.pth"
            if os.path.exists(model_path):
                dqn_model.load_state_dict(torch.load(model_path,weights_only=True))
                print(f"Loaded model for agent {agent_id}")
            else:
                print(f"No saved model found for agent {agent_id}, starting with a new model.")     


        while data:
            # Decode JSON data
            data_dict = json.loads(data.decode())
            agent_id = data_dict.get('agent_id')
            state_dict = data_dict.get('state')
            reward = data_dict.get('reward')
            done = data_dict.get('done')

            if(done):
                print("asdfasdf")
            # Prepare state values for DQN input
            state_values = np.array([
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
            
            #if step % 64 == 0:
                #print(f"Message received from {addr}: {data_dict}")

            # Select action
            action = agent.select_action(state_values, epsilon)

            
            if episode %render_episode_frequency ==0:
                render_episode = True

            # Send the selected action back to Unity
            response = json.dumps({
                "agent_id": agent_id,
                "action": action,
                "render" : render_episode
            })
            conn.sendall(response.encode())

            # Receive next state data
            next_data = conn.recv(1024)
            if not next_data:
                break
            next_data_dict = json.loads(next_data.decode())

            # Extract next state information
            next_state_dict = next_data_dict.get('state')
            next_state_values = np.array([
                    next_state_dict['gold'],
                    next_state_dict['health'],
                    next_state_dict['miners'],
                    next_state_dict['swordsmen'],
                    next_state_dict['archers'],
                    next_state_dict['stateValue'],
                    next_state_dict['nearby_resources_available'],
                    next_state_dict['enemy_health'],
                    next_state_dict['enemy_miners'],
                    next_state_dict['enemy_swordsmen'],
                    next_state_dict['enemy_archers'],
                    next_state_dict['enemies_in_vicinity'],
                    next_state_dict['episode_time']
                ], dtype=np.float32)

            # Store experience in replay memory
            replay_memory.push(state_values, action, reward, next_state_values, done)

            # Optimize the model
            agent.optimize_model(batch_size)

            # Update target network periodically
            if step % target_update_frequency == 0:
                target_dqn_model.load_state_dict(dqn_model.state_dict())

            if step % model_save_frequency == 0:
                    torch.save(dqn_model.state_dict(), model_path)
                    #print(f"Model saved for agent {agent_id} after {step} steps.")

            # Decay epsilon
            if epsilon > epsilon_min:
                epsilon *= epsilon_decay

            step += 1
            total_reward += reward

            if done:
                # Episode has ended
                print(f"Episode {episode}, Total Reward: {total_reward}, Epsilon: {epsilon}")
                total_reward = 0
                episode += 1
                step = 0  # Optionally reset step for new episode
                render_episode = False
                print(f"Message received from {agent_id}: {data_dict} : this is next data\n\n{next_data_dict}")

            # Update data for next iteration
            data = next_data

    except Exception as e:
        print(f"Error handling client {addr}: {e}")
    finally:
        if 'agent_id' in locals():  # Check if agent_id was defined
            torch.save(dqn_model.state_dict(), model_path)
            print(f"Final model saved for agent {agent_id}")
        print(f"Connection with {addr} closed.")
        conn.close()

# Main server function
def main():
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
    main()
