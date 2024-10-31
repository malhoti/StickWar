import json
import socket
import torch
import torch.optim as optim
# from dqn import DQN  # Import DQN model class from dqn.py
# from replay_buffer import ReplayBuffer  # Import replay buffer class
# from training import train_dqn  # Import training function

# Hyperparameters
input_dim = 8  # Example input dimension, adjust based on your state vector
output_dim = 7  # Example output dimension (number of actions)
gamma = 0.99  # Discount factor for future rewards
batch_size = 64
learning_rate = 0.001
epsilon = 1.0  # Initial epsilon for exploration
epsilon_min = 0.1
epsilon_decay = 0.995
target_update_frequency = 1000  # Update target network every 1000 steps
buffer_capacity = 10000

# Initialize models, optimizer, and replay buffer
# dqn_model = DQN(input_dim, output_dim)
# target_dqn_model = DQN(input_dim, output_dim)
# target_dqn_model.load_state_dict(dqn_model.state_dict())  # Copy weights to target model initially
# target_dqn_model.eval()  # Target network is in evaluation mode
# optimizer = optim.Adam(dqn_model.parameters(), lr=learning_rate)
# replay_buffer = ReplayBuffer(buffer_capacity)

# Server configuration
HOST = '192.168.1.206'  # Localhost
PORT = 5000         # Port to bind the server to

def main():
    # Set up the server socket
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
        server_socket.bind((HOST, PORT))
        server_socket.listen()
        print("Server is listening...")

        conn, addr = server_socket.accept()  # Wait for Unity connection
        with conn:
            print(f"Connected by {addr}")
            step = 0  # Track steps for target network update

            while True:
                # Step 1: Receive data from Unity
                data = conn.recv(1024)
                if not data:
                    break

                # Decode JSON data
                data_dict = json.loads(data.decode())
             
                message = data_dict['message']
                
                print(f"Messaged Recieved from {addr}: {message}")

                # Step 3: Send the selected action back to Unity
                response = json.dumps({"message": "I am From Python"})
                conn.sendall(response.encode())

                
                step += 1

if __name__ == "__main__":
    main()
