from collections import deque
import random
import numpy as np
class ReplayMemory(object):

    def __init__(self, capacity):
        self.memory = deque([], maxlen=capacity)

    def push(self, state, action, reward, next_state, done):
        experience = (state, action, reward, next_state, done)
        self.memory.append(experience)

    def sample(self, batch_size):
        # Step 1: Sample a batch of experiences
        experiences = random.sample(self.memory, batch_size)
        
        # Step 2: Transpose the list of tuples into individual lists
        states, actions, rewards, next_states, dones = zip(*experiences)
        
        # Step 3: Convert each component into a NumPy array
        states = np.array(states)
        actions = np.array(actions)
        rewards = np.array(rewards)
        next_states = np.array(next_states)
        dones = np.array(dones)

        return states, actions, rewards, next_states, dones

    def __len__(self):
        return len(self.memory)