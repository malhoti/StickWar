
#import os

import torch
import torch.optim as optim
import torch.nn.functional as F

import numpy as np
from DQN import DQN
import torch
import os
import numpy as np
import torch.nn.functional as F

class DQNAgent:
    def __init__(self, dqn_model, target_dqn_model, optimizer, replay_memory, device, output_dim, gamma=0.99):
        self.dqn_model = dqn_model
        self.target_dqn_model = target_dqn_model
        self.optimizer = optimizer
        self.replay_memory = replay_memory
        self.device = device
        self.output_dim = output_dim
        self.gamma = gamma

    def select_action(self, state, epsilon):
        # Epsilon-greedy action selection
        if np.random.rand() < epsilon:
            return np.random.randint(self.output_dim)  # Random action
        else:
            state = torch.tensor(state, dtype=torch.float32).unsqueeze(0).to(self.device)
            with torch.no_grad():
                q_values = self.dqn_model(state)
            return q_values.argmax().item()  # Action with max Q-value

    def optimize_model(self, batch_size):
        if len(self.replay_memory) < batch_size:
            return  # Not enough samples to train

        # Sample a batch from the replay buffer
        states, actions, rewards, next_states, dones = self.replay_memory.sample(batch_size)

        # Convert to tensors
        states = torch.tensor(states, dtype=torch.float32).to(self.device)
        actions = torch.tensor(actions, dtype=torch.int64).unsqueeze(1).to(self.device)
        rewards = torch.tensor(rewards, dtype=torch.float32).unsqueeze(1).to(self.device)
        next_states = torch.tensor(next_states, dtype=torch.float32).to(self.device)
        dones = torch.tensor(dones, dtype=torch.float32).unsqueeze(1).to(self.device)

        # Compute current Q values
        q_values = self.dqn_model(states).gather(1, actions)

        # Compute target Q values
        with torch.no_grad():
            max_next_q_values = self.target_dqn_model(next_states).max(1)[0].unsqueeze(1)
        target_q_values = rewards + (self.gamma * max_next_q_values * (1 - dones))

        # Compute loss
        loss = F.mse_loss(q_values, target_q_values)

        # Optimize the model
        self.optimizer.zero_grad()
        loss.backward()
        self.optimizer.step()

    def load_model(self, agent_id):
        model_path = f"models/model_agent_{agent_id}.pth"
        if os.path.exists(model_path):
            self.dqn_model.load_state_dict(torch.load(model_path))
            print(f"Loaded model for agent {agent_id} from {model_path}")
        else:
            print(f"No saved model found for agent {agent_id}, starting with a new model.")

    def save_model(self, agent_id):
        model_path = f"models/model_agent_{agent_id}.pth"
        os.makedirs("models", exist_ok=True)  # Create the directory if it doesn't exist
        torch.save(self.dqn_model.state_dict(), model_path)
        print(f"Saved model for agent {agent_id} to {model_path}")

