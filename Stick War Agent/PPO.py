import torch
import torch.nn as nn

class PPO(nn.Module):
    def __init__(self, input_dim, output_dim):
        super(PPO, self).__init__()
        self.fc1 = nn.Linear(input_dim, 128)
        self.fc2 = nn.Linear(128, 64)
        # Actor head: outputs logits for each action
        self.policy_head = nn.Linear(64, output_dim)
        # Critic head: outputs state value
        self.value_head = nn.Linear(64, 1)
    
    def forward(self, x):
        x = torch.relu(self.fc1(x))
        x = torch.relu(self.fc2(x))
        policy_logits = self.policy_head(x) # raw action score, polocy then being the probability of choosing that action
        value = self.value_head(x) # estemated score of the state
        return policy_logits, value