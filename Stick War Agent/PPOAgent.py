import torch
import torch.nn.functional as F
import os
import numpy as np
class PPOAgent:
    def __init__(self, model, optimizer, clip_epsilon, gamma,
                 update_epochs, value_coef,
                 entropy_coef, device='cpu'):
        self.model = model
        self.optimizer = optimizer
        self.clip_epsilon = clip_epsilon
        self.gamma = gamma
        self.update_epochs = update_epochs
        self.value_coef = value_coef
        self.entropy_coef = entropy_coef
        self.device = device
        self.memory = []  # To store trajectories (on-policy)
        

    def select_action(self, state):
        """Given a state, sample an action from the policy."""
        state_tensor = torch.tensor(state, dtype=torch.float32).unsqueeze(0).to(self.device)
        policy_logits, value = self.model(state_tensor) # calls the forward function on model
        policy = torch.softmax(policy_logits, dim=-1)  # converts the raw scores into probabliies
        dist = torch.distributions.Categorical(policy)
        action = dist.sample()
        log_prob = dist.log_prob(action)

        return action.item(), log_prob.detach(), value.detach()

    def store_transition(self, transition):
        """Store a single transition.
        Transition is a tuple: (state, action, reward, log_prob, value, done)
        """
        self.memory.append(transition)

    def compute_returns_and_advantages(self, last_value, done, lambda_=0.95):
        returns = []
        advantages = []
        R = 0 if done else last_value.item() # if episode is done then future reward is 0
        gae = 0  # Initialize GAE estimate
        values = [t[4].item() for t in self.memory] + [R]

        for i in reversed(range(len(self.memory))):
            _, _, reward, _, value, _ = self.memory[i]
            delta = reward + self.gamma * values[i + 1] - values[i]
            gae = delta + self.gamma * lambda_ * gae
            advantages.insert(0, gae)
            returns.insert(0, gae + value)
        returns = torch.tensor(returns, dtype=torch.float32).to(self.device)
        advantages = torch.tensor(advantages, dtype=torch.float32).to(self.device)
        if advantages.numel() > 1:
            advantages = (advantages - advantages.mean()) / (advantages.std() + 1e-8)
        else:
            advantages = advantages - advantages.mean()
        
        return returns, advantages


    def update(self, returns, advantages):
        # Convert list of numpy arrays to a single numpy array first
        states_np = np.array([t[0] for t in self.memory])
        states = torch.tensor(states_np, dtype=torch.float32).to(self.device)
        
        actions_np = np.array([t[1] for t in self.memory])
        actions = torch.tensor(actions_np, dtype=torch.int64).to(self.device)

        
        # For old_log_probs, we extract the float values.
        old_log_probs_np = np.array([t[3].item() for t in self.memory])
        old_log_probs = torch.tensor(old_log_probs_np, dtype=torch.float32).to(self.device)

        total_policy_loss = 0.0
        total_value_loss = 0.0
        total_entropy = 0.0

        for _ in range(self.update_epochs):
            policy_logits, values = self.model(states)
            policy = torch.softmax(policy_logits, dim=-1)
            dist = torch.distributions.Categorical(policy)
            new_log_probs = dist.log_prob(actions)
            entropy = dist.entropy().mean()

            # Compute ratio (new prob / old prob)
            ratio = torch.exp(new_log_probs - old_log_probs)

            # compute clipped surrogate objective
            surr1 = ratio * advantages
            surr2 = torch.clamp(ratio, 1.0 - self.clip_epsilon, 1.0 + self.clip_epsilon) * advantages

            # Perform gradient ascent 
            policy_loss = -torch.min(surr1, surr2).mean()
            value_loss = F.mse_loss(values.squeeze(1), returns)
            loss = policy_loss + self.value_coef * value_loss - self.entropy_coef * entropy

            self.optimizer.zero_grad()
            loss.backward()
            torch.nn.utils.clip_grad_norm_(self.model.parameters(), max_norm=0.5)
            self.optimizer.step()

            total_policy_loss += policy_loss.item()
            total_value_loss += value_loss.item()
            total_entropy += entropy.item()

        self.policy_loss = total_policy_loss / self.update_epochs
        self.value_loss = total_value_loss / self.update_epochs
        self.entropy = total_entropy / self.update_epochs
        self.total_loss = self.policy_loss + self.value_coef * self.value_loss - self.entropy_coef * self.entropy
        # Clear memory after update
        self.memory = []


    def save_model(self, agent_id, model_path=None):
        if model_path is None:
            model_path = f"models/PPO_agent_v2.pth"
        os.makedirs("models", exist_ok=True)
        torch.save(self.model.state_dict(), model_path)
        print(f"Saved model for agent {agent_id} to {model_path}")

    def load_model(self, agent_id, model_path):
        print(model_path)
        if os.path.exists(model_path):
            self.model.load_state_dict(torch.load(model_path))
            print(f"Loaded model for agent {agent_id} from {model_path}")
        else:
            print(f"No saved model found for agent {agent_id}, starting with a new model.")