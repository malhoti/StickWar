

import matplotlib.pyplot as plt
import queue

import os

def plot_rewards(agent_id, episode_rewards):
    """Plot and save a line graph for the total rewards over episodes."""
    os.makedirs("graphs", exist_ok=True)
    plt.figure()
    plt.plot(range(1, len(episode_rewards) + 1), episode_rewards, marker='o', linestyle='-')
    plt.xlabel('Episode')
    plt.ylabel('Total Reward')
    plt.title(f'Agent {agent_id} Total Reward per Episode')
    plt.savefig(f"graphs/agent_{agent_id}_reward_plot.png")
    plt.close()


def live_plot(reward_queue,step):
    plt.ion()  # interactive mode on
    fig, ax = plt.subplots()
    
    # Dictionary to hold reward lists for each agent
    agent_rewards = {}
    
    while True:
        # Try to get new reward data (non-blocking, with a small timeout)
        try:
            agent_id, reward = reward_queue.get(timeout=0.1)
            if agent_id not in agent_rewards:
                agent_rewards[agent_id] = []
            agent_rewards[agent_id].append(reward)
        except queue.Empty:
            pass

        # Clear the axis and plot the rewards for each agent
        ax.clear()
        for aid, rewards in agent_rewards.items():
            ax.plot(range(0, len(rewards)* step, step), rewards, marker='o', label=f'Agent {aid}')
        
        ax.set_xlabel('Step (100)')
        ax.set_ylabel('Total Reward')
        ax.set_title('Live Reward per Episode')
        
        plt.draw()
        plt.pause(0.1)

