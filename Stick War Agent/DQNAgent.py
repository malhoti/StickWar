import torch
import torch.nn.functional as F
import numpy as np
import os

class DQNAgent:
    def __init__(self,
                 dqn_model,
                 target_dqn_model,
                 optimizer,
                 replay_memory,
                 device,
                 output_dim,
                 gamma=0.99,
                 # --- new hyperparameters below ---
                 epsilon= 1.0,
                 epsilon_end= 0.01,
                 epsilon_decay= 0.99999,
                 target_update_freq= 2000,
                 gradient_clip= 1.0):
        # core networks & optimizer
        self.dqn_model = dqn_model
        self.target_dqn_model = target_dqn_model
        self.optimizer = optimizer

        # replay buffer
        self.replay_memory = replay_memory

        # device & dims
        self.device = device
        self.output_dim = output_dim

        # discount factor
        self.gamma = gamma

        # --- exploration schedule ---
        self.epsilon = epsilon
        self.epsilon_end = epsilon_end
        self.epsilon_decay = epsilon_decay

        # --- target‐network update schedule ---
        self.target_update_freq = target_update_freq
        self.update_count = 0

        # --- gradient clipping ---
        self.gradient_clip = gradient_clip

        # for unified training loop compatibility
        self.memory = replay_memory
        self.model = dqn_model

        # initialize loss attribute
        self.total_loss = 0.0

    def select_action(self, state):
        """
        Epsilon‐greedy: with prob ε pick random action,
        otherwise pick argmax_a Q(s,a).  Then decay ε.
        Returns (action, dummy_log_prob, dummy_value).
        """
        if np.random.rand() < self.epsilon:
            action = np.random.randint(self.output_dim)
        else:
            st = torch.tensor(np.array(state), dtype=torch.float32, device=self.device)
            with torch.no_grad():
                q_vals = self.dqn_model(st.unsqueeze(0))
            action = q_vals.argmax().item()

        # decay for next call
        self.epsilon = max(self.epsilon_end, self.epsilon * self.epsilon_decay)

        return action, 0, 0

    def store_transition(self, transition):
        """
        transition: (state, action, reward, next_state, done)
        """
        self.replay_memory.push(transition)

    def update(self, batch_size):
        """
        Sample a batch, compute TD‐targets, do one gradient step,
        clip gradients, and update target network periodically.
        """
        if len(self.replay_memory) < batch_size * 5:
            return

        batch = self.replay_memory.sample(batch_size)
        states, actions, rewards, next_states, dones = zip(*batch)

        # fast conversion to tensors
        states      = torch.tensor(np.array(states),      dtype=torch.float32, device=self.device)
        actions     = torch.tensor(np.array(actions),     dtype=torch.int64,   device=self.device).unsqueeze(1)
        rewards     = torch.tensor(np.array(rewards),     dtype=torch.float32, device=self.device).unsqueeze(1)
        next_states = torch.tensor(np.array(next_states), dtype=torch.float32, device=self.device)
        dones       = torch.tensor(np.array(dones),       dtype=torch.float32, device=self.device).unsqueeze(1)

        # current Q-values for chosen actions
        current_q = self.dqn_model(states).gather(1, actions)

        # compute TD target via the target network
        with torch.no_grad():
            # (1) select best action according to the *online* network
            best_next_actions = self.dqn_model(next_states).argmax(dim=1, keepdim=True)
            # (2) evaluate those actions using the *target* network
            max_next_q = self.target_dqn_model(next_states).gather(1, best_next_actions)

        target_q = rewards + self.gamma * max_next_q * (1 - dones)

        # MSE loss
        loss = F.mse_loss(current_q, target_q)

        # optimize
        self.optimizer.zero_grad()
        loss.backward()
        torch.nn.utils.clip_grad_norm_(self.dqn_model.parameters(), self.gradient_clip)
        self.optimizer.step()

        # logging & target‐network update
        self.total_loss = loss.item()
        self.update_count += 1
        if self.update_count % self.target_update_freq == 0:
            self.target_dqn_model.load_state_dict(self.dqn_model.state_dict())

    def save_model(self, agent_id, model_path=None):
        if model_path is None:
            model_path = f"models/DQN_agent_v2.pth"
        os.makedirs("models", exist_ok=True)
        torch.save(self.dqn_model.state_dict(), model_path)
        print(f"Saved model for agent {agent_id} to {model_path}")

    def load_model(self, agent_id, model_path=None):
        if model_path is None:
            model_path = f"models/DQN_agent_{agent_id}.pth"
        if os.path.exists(model_path):
            self.dqn_model.load_state_dict(torch.load(model_path))
            print(f"Loaded model for agent {agent_id} from {model_path}")
        else:
            print(f"No saved model found for agent {agent_id}, starting fresh.")
