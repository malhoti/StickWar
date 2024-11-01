import json
import socket
import torch
import torch.optim as optim
import torch.nn.functional as F
import random
import numpy as np
from DQN import DQN
from replayMemory import ReplayMemory
# from dqn import DQN  # Import DQN model class from dqn.py
# from replay_buffer import ReplayMemory  # Import replay buffer class
# from training import train_dqn  # Import training function

# Hyperparameters
input_dim = 7 # Example input dimension, adjust based on your state vector
output_dim = 6  # Example output dimension (number of actions)
gamma = 0.99  # Discount factor for future rewards
batch_size = 64
learning_rate = 0.001
epsilon = 1.0  # Initial epsilon for exploration
epsilon_min = 0.1
epsilon_decay = 0.995
target_update_frequency = 1000  # Update target network every 1000 steps
buffer_capacity = 10000

# Initialize models, optimizer, and replay buffer
dqn_model = DQN(input_dim, output_dim)
target_dqn_model = DQN(input_dim, output_dim)
target_dqn_model.load_state_dict(dqn_model.state_dict())  # Copy weights to target model initially
target_dqn_model.eval()  # Target network is in evaluation mode
optimizer = optim.Adam(dqn_model.parameters(), lr=learning_rate)
replay_memory = ReplayMemory(buffer_capacity)

# Server configuration
HOST = '192.168.1.206'  # Localhost
PORT = 5000         # Port to bind the server to


device = torch.device(
    "cuda" if torch.cuda.is_available() else
    "mps" if torch.backends.mps.is_available() else
    "cpu"
)

def select_action(state, epsilon):
    # Epsilon-greedy action selection
    if np.random.rand() < epsilon:
        # Exploration: select a random action
        return np.random.randint(output_dim)
    else:
        # Exploitation: select the action with max Q-value
        state = torch.tensor(state, dtype=torch.float32).unsqueeze(0).to(device)
        with torch.no_grad():
            q_values = dqn_model(state)
        return q_values.argmax().item()

def optimize_model():
    if len(replay_memory) < batch_size:
        return  # Not enough samples to train

    # Sample a batch from the replay buffer
    
    states, actions, rewards, next_states, dones = replay_memory.sample(batch_size)

    # Convert to tensors
    states = torch.tensor(states, dtype=torch.float32).to(device)
    actions = torch.tensor(actions, dtype=torch.int64).unsqueeze(1).to(device)
    rewards = torch.tensor(rewards, dtype=torch.float32).unsqueeze(1).to(device)
    next_states = torch.tensor(next_states, dtype=torch.float32).to(device)
    dones = torch.tensor(dones, dtype=torch.float32).unsqueeze(1).to(device)

    # Compute current Q values
    q_values = dqn_model(states).gather(1, actions)

    # Compute target Q values
    with torch.no_grad():
        max_next_q_values = target_dqn_model(next_states).max(1)[0].unsqueeze(1)
    target_q_values = rewards + (gamma * max_next_q_values * (1 - dones))

    # Compute loss
    loss = F.mse_loss(q_values, target_q_values)

    # Optimize the model
    optimizer.zero_grad()
    loss.backward()
    optimizer.step()

def main():
    global epsilon
    
    # Set up the server socket
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
        server_socket.bind((HOST, PORT))
        server_socket.listen()
        print("Server is listening...")
        
        conn, addr = server_socket.accept()  # Wait for Unity connection
        with conn:
            print(f"Connected by {addr}")
            step = 0  # Track steps for target network update
            totalReward = 0
            episode = 0
            
            data = conn.recv(1024)
                    
                
            while True:

                # Decode JSON data
                data_dict = json.loads(data.decode())

                agent_id = data_dict.get('agent_id')
                state_dict = data_dict.get('state')
                reward = data_dict.get('reward')
                done = data_dict.get('done')


                state_values = np.array([
                    state_dict['gold'],
                    state_dict['health'],
                    state_dict['miners'],
                    state_dict['swordsmen'],
                    state_dict['archers'],
                    state_dict['stateValue'],
                    state_dict['enemies_in_vicinity']
                ], dtype=np.float32)
                
                if (step % 64 == 0):
                    print(f"Messaged Recieved from {addr}: {data_dict}")

                action = select_action(state_values, epsilon)
                

                # Step 3: Send the selected action back to Unity
                response = json.dumps({
                    "agent_id": agent_id,
                    "action": action
                })
                conn.sendall(response.encode())


                next_data = conn.recv(1024)
                if not next_data:
                    break
                next_data_dict = json.loads(next_data.decode())

                #print(f"Messaged Recieved from Unity after action was played out: {next_data_dict}")

                # Extract next state information
                next_state_dict = next_data_dict.get('state')
                next_state_values = np.array([
                    next_state_dict['gold'],
                    next_state_dict['health'],
                    next_state_dict['miners'],
                    next_state_dict['swordsmen'],
                    next_state_dict['archers'],
                    next_state_dict['stateValue'],
                    next_state_dict['enemies_in_vicinity']
                ], dtype=np.float32)

               
                replay_memory.push(state_values, action, reward, next_state_values, done)

                  # Optimize the model
                optimize_model()

                # Update target network periodically
                if step % target_update_frequency == 0:
                    target_dqn_model.load_state_dict(dqn_model.state_dict())

                # Decay epsilon
                if epsilon > epsilon_min:
                    epsilon *= epsilon_decay

                step += 1
                totalReward += reward

                if done:
                    # Episode has ended
                    print(f"Episode {episode}, Total Reward: {total_reward}, Epsilon: {epsilon}")
                    total_reward = 0
                    episode += 1
                    # Optionally, reset any necessary variables
                    # Break or continue based on your setup
                    # For continuous training, you might reset 'step' and 'epsilon' here
                    # For now, we'll reset 'step'
                    step = 0

                data = next_data
                step += 1

if __name__ == "__main__":
    main()
