import socket
import json
import torch
import numpy as np
from collections import deque
from dqn_model import DQN  # Your DQN model here

# Set up socket server
server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
server.bind(('localhost', 5000))
server.listen(1)
client, addr = server.accept()

# Set up DQN and other parameters
dqn_model = DQN()
replay_buffer = deque(maxlen=10000)
epsilon = 1.0  # Epsilon for exploration-exploitation

def receive_from_unity():
    data = client.recv(1024).decode('utf-8')
    return json.loads(data)

def send_to_unity(action):
    client.send(str(action).encode('utf-8'))

def train_dqn():
    # Sample from replay buffer and train the model
    pass

while True:
    # Receive observations and reward from Unity
    data = receive_from_unity()
    observations = np.array(data["observations"])
    reward = data["reward"]

    # Choose action with epsilon-greedy policy
    if np.random.rand() < epsilon:
        action = np.random.choice([0, 1, 2, 3, 4, 5])  # Random action for exploration
    else:
        action = dqn_model.predict(observations)  # Choose action with DQN

    # Store experience in replay buffer
    replay_buffer.append((observations, action, reward))

    # Train DQN if enough samples
    if len(replay_buffer) > batch_size:
        train_dqn()

    # Decay epsilon
    epsilon = max(0.1, epsilon * 0.995)

    # Send action to Unity
    send_to_unity(action)
