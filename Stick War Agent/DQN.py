import torch
import torch.nn as nn
import torch.optim as optim
import torch.autograd as grad

device = torch.device(
    "cuda" if torch.cuda.is_available() else
    "mps" if torch.backends.mps.is_available() else
    "cpu"
)

class DQN(nn.Module):
    def __init__(self, input_dim, output_dim):
        super(DQN, self).__init__()
        
        # Define the layers of the neural network
        self.fc1 = nn.Linear(input_dim, 128)  # First hidden layer
        self.fc2 = nn.Linear(128, 64)         # Second hidden layer
        self.fc3 = nn.Linear(64, output_dim)  # Output layer with Q-values for each action

    def forward(self, x):
        x = torch.relu(self.fc1(x))
        x = torch.relu(self.fc2(x))
        return self.fc3(x)  # Output layer returns raw Q-values for each action

    def save_model(self, filepath):
        torch.save(self.state_dict(), filepath)
        
    # Load model weights
    def load_model(self, filepath):
        self.load_state_dict(torch.load(filepath))