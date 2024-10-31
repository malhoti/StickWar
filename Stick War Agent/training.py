import torch
import torch.nn as nn
import torch.optim as optim
import torch.autograd as grad
import numpy as np
from DQN import DQN  # Assuming DQN is in dqn.py
from replayMemory import ReplayMemory  # Assuming ReplayBuffer is in replay_buffer.py

device = torch.device(
    "cuda" if torch.cuda.is_available() else
    "mps" if torch.backends.mps.is_available() else
    "cpu"
)


# Hyperparameters
input_dim = 10           # Dimension of state representation
output_dim = 6           # Number of actions
learning_rate = 0.001
gamma = 0.99             # Discount factor for future rewards
batch_size = 64
epsilon = 1.0            # Initial exploration rate
epsilon_min = 0.1
epsilon_decay = 0.995
target_update_freq = 1000  # Frequency for updating the target network
max_memory_size = 10000

# Initialize networks
policy_net = DQN(input_dim, output_dim).to(device)
target_net = DQN(input_dim, output_dim).to(device)
target_net.load_state_dict(policy_net.state_dict())
target_net.eval()  # Target network is initially the same as policy but stays stable

# Initialize optimizer and replay buffer
optimizer = optim.Adam(policy_net.parameters(), lr=learning_rate)
replay_buffer = ReplayMemory(max_memory_size)

# Function to select action using epsilon-greedy policy
def select_action(state, epsilon):
    if np.random.rand() < epsilon:
        return np.random.randint(output_dim)  # Random action (explore)
    else:
        with torch.no_grad():
            return policy_net(state.to(device)).argmax().item()  # Greedy action (exploit)

# Training loop
num_episodes = 1000
for episode in range(num_episodes):
    #state = env.reset()  # Reset environment and get initial state
    done = False
    total_reward = 0

    while not done:
        # Convert state to tensor
        state_tensor = torch.tensor(state, dtype=torch.float32).unsqueeze(0).to(device)

        # Select action
        action = select_action(state_tensor, epsilon)

        # Take action in the environment
        #next_state, reward, done, _ = env.step(action)
        total_reward += reward

        # Store experience in replay buffer
        replay_buffer.push(state, action, reward, next_state, done)

        # Update state
        state = next_state

        # Training step: sample a batch and update policy network
        if len(replay_buffer) > batch_size:
            states, actions, rewards, next_states, dones = replay_buffer.sample(batch_size)

            # Convert to tensors
            states = torch.tensor(states, dtype=torch.float32).to(device)
            actions = torch.tensor(actions, dtype=torch.int64).unsqueeze(1).to(device)
            rewards = torch.tensor(rewards, dtype=torch.float32).to(device)
            next_states = torch.tensor(next_states, dtype=torch.float32).to(device)
            dones = torch.tensor(dones, dtype=torch.float32).to(device)

            # Compute Q values for current states
            q_values = policy_net(states).gather(1, actions)

            # Compute target Q values for next states
            next_q_values = target_net(next_states).max(1)[0].detach()
            target_q_values = rewards + (gamma * next_q_values * (1 - dones))

            # Compute loss
            loss = nn.MSELoss()(q_values, target_q_values.unsqueeze(1))

            # Optimize the model
            optimizer.zero_grad()
            loss.backward()
            optimizer.step()

        # Update target network periodically
        if episode % target_update_freq == 0:
            target_net.load_state_dict(policy_net.state_dict())

    # Decay epsilon
    if epsilon > epsilon_min:
        epsilon *= epsilon_decay

    print(f"Episode {episode}, Total Reward: {total_reward}, Epsilon: {epsilon}")

