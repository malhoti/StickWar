

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
        ax.legend()
        plt.draw()
        plt.pause(0.1)
# 1. Plot total reward per episode.
def plot_rewards(agent_id, episode_rewards):
    os.makedirs("graphs", exist_ok=True)
    plt.figure()
    plt.plot(range(1, len(episode_rewards) + 1), episode_rewards, marker='o', linestyle='-')
    plt.xlabel('Episode')
    plt.ylabel('Total Reward')
    plt.title(f'Agent {agent_id} Total Reward per Episode')
    plt.savefig(f"graphs/agent_{agent_id}_reward_plot.png")
    plt.close()

# 2. Plot average reward per step over episodes.
def plot_average_reward(agent_id, episode_avg_rewards):
    os.makedirs("graphs", exist_ok=True)
    plt.figure()
    plt.plot(range(1, len(episode_avg_rewards) + 1), episode_avg_rewards, marker='o', linestyle='-')
    plt.xlabel('Episode')
    plt.ylabel('Average Reward per Step')
    plt.title(f'Agent {agent_id} Average Reward per Episode')
    plt.savefig(f"graphs/agent_{agent_id}_avg_reward_plot.png")
    plt.close()

# 3. Plot loss curves (policy, value, total) over episodes.
def plot_loss_curves(agent_id, episodes, policy_losses, value_losses, total_losses):
    os.makedirs("graphs", exist_ok=True)
    plt.figure()
    plt.plot(episodes, policy_losses, label="Policy Loss")
    plt.plot(episodes, value_losses, label="Value Loss")
    plt.plot(episodes, total_losses, label="Total Loss")
    plt.xlabel("Episode")
    plt.ylabel("Loss")
    plt.title(f"Agent {agent_id} Loss Curves")
    plt.legend()
    plt.savefig(f"graphs/agent_{agent_id}_loss_plot.png")
    plt.close()

# 4. Plot entropy over episodes.
def plot_entropy(agent_id, episodes, entropies):
    os.makedirs("graphs", exist_ok=True)
    plt.figure()
    plt.plot(episodes, entropies, marker='o', linestyle='-')
    plt.xlabel("Episode")
    plt.ylabel("Entropy")
    plt.title(f"Agent {agent_id} Policy Entropy Over Episodes")
    plt.savefig(f"graphs/agent_{agent_id}_entropy_plot.png")
    plt.close()

# 5. Plot action distribution as a histogram.
def plot_action_distribution(agent_id, action_counts):
    os.makedirs("graphs", exist_ok=True)
    actions = list(action_counts.keys())
    counts = [action_counts[a] for a in actions]
    plt.figure()
    plt.bar(actions, counts)
    plt.xlabel("Action")
    plt.ylabel("Frequency")
    plt.title(f"Agent {agent_id} Action Distribution")
    plt.savefig(f"graphs/agent_{agent_id}_action_distribution.png")
    plt.close()

# Combined Plot: 4 subplots (2x2 grid)
# Subplot 1: Total Reward per Episode
# Subplot 2: Average Reward per Episode (per step)
# Subplot 3: Loss Curves (Policy, Value, Total)
# Subplot 4: Entropy & Action Distribution (twin axes)
def plot_all_metrics(agent_id, episode_rewards, episodes, policy_losses, value_losses, total_losses, entropies, action_counts):
    os.makedirs("graphs", exist_ok=True)
    fig, axs = plt.subplots(2, 2, figsize=(14, 10))

    # Subplot 1: Total Reward
    axs[0, 0].plot(range(1, len(episode_rewards) + 1), episode_rewards, marker='o', linestyle='-')
    axs[0, 0].set_xlabel("Episode")
    axs[0, 0].set_ylabel("Total Reward")
    axs[0, 0].set_title(f"Agents Total Reward")

    # Subplot 2: Average Reward per Step
    # axs[0, 1].plot(range(1, len(episode_avg_rewards) + 1), episode_avg_rewards, marker='o', linestyle='-')
    # axs[0, 1].set_xlabel("Episode")
    # axs[0, 1].set_ylabel("Average Reward/Step")
    # axs[0, 1].set_title("Average Reward per Step")

    # Subplot 3: Loss Curves
    axs[1, 0].plot(episodes, policy_losses, label="Policy Loss")
    axs[1, 0].plot(episodes, value_losses, label="Value Loss")
    axs[1, 0].plot(episodes, total_losses, label="Total Loss")
    axs[1, 0].set_xlabel("Episode")
    axs[1, 0].set_ylabel("Loss")
    axs[1, 0].set_title("Loss Curves")
    axs[1, 0].legend()

    # Subplot 4: Entropy & Action Distribution (twin axes)
    ax4 = axs[1, 1]
    # Plot entropy on primary y-axis.
    ax4.plot(episodes, entropies, color="blue", marker='o', linestyle='-', label="Entropy")
    ax4.set_xlabel("Episode")
    ax4.set_ylabel("Entropy", color="blue")
    ax4.tick_params(axis="y", labelcolor="blue")
    ax4.set_title("Entropy & Action Distribution")
    
    # Plot action distribution on secondary y-axis.
    ax4b = ax4.twinx()
    actions = list(action_counts.keys())
    counts = [action_counts[a] for a in actions]
    ax4b.bar(actions, counts, color="orange", alpha=0.5, label="Action Frequency")
    ax4b.set_ylabel("Action Frequency", color="orange")
    ax4b.tick_params(axis="y", labelcolor="orange")

    fig.tight_layout(rect=[0, 0, 1, 0.95])
    plt.suptitle(f"Agent {agent_id} Training Metrics", fontsize=16)
    plt.savefig(f"graphs/agent_{agent_id}_all_metrics.png")
    plt.close()




def live_plot_all_metrics( all_metrics, update_interval=5):
    
    """
    Live plot function to update a 2x2 grid of subplots with training metrics.
    
    Parameters:
      agent_id: The ID of the agent to plot metrics for.
      get_metrics_func: A function that returns a dictionary with keys:
          - "episode_rewards": list of total rewards per episode,
          - "episode_avg_rewards": list of average rewards per episode,
          - "episodes": list of episode numbers,
          - "policy_losses": list of policy loss values,
          - "value_losses": list of value loss values,
          - "total_losses": list of total loss values,
          - "entropies": list of entropy values,
          - "action_counts": dictionary mapping action indices to counts.
      update_interval: How frequently (in seconds) to update the plot.
    """
    os.makedirs("graphs", exist_ok=True)
    plt.ion()  # Turn on interactive mode
    fig, axs = plt.subplots(2, 2, figsize=(14, 10))
    print(all_metrics)
    while True:

        # if agent_id not in all_metrics or not all_metrics[agent_id]:
        #     plt.pause(0.1)
        #     continue


        # Get current metrics for this agent.
        
        for agent_id, metrics in all_metrics.items():
            episode_rewards    = metrics.get("episode_rewards", [])
        
            episodes           = metrics.get("episodes", list(range(1, len(episode_rewards)+1)))
            policy_losses      = metrics.get("policy_losses", [])
            value_losses       = metrics.get("value_losses", [])
            total_losses       = metrics.get("total_losses", [])
            entropies          = metrics.get("entropies", [])
            action_counts      = metrics.get("action_counts", {})

            axs[0, 0].plot(episodes, episode_rewards, marker='o', linestyle='-', label=f"Agent {agent_id}")

            axs[0, 1].plot(episodes, policy_losses, label=f"Policy Loss Agent {agent_id}")
            axs[0, 1].plot(episodes, value_losses, label=f"Value Loss Agent {agent_id}")
            axs[0, 1].plot(episodes, total_losses, label=f"Total Loss Agent {agent_id}")

        # Extract metrics (with default fallbacks if not present)
      
        

        # --- Subplot 1: Total Reward per Episode ---
        axs[0, 0].clear()
       
        axs[0, 0].set_xlabel("Episode")
        axs[0, 0].set_ylabel("Total Reward")
        axs[0, 0].set_title(f"Agents Total Reward")
        
        
        # --- Subplot 2: Loss Curves ---
        axs[0, 1].clear()
        
        axs[0, 1].set_xlabel("Episode")
        axs[0, 1].set_ylabel("Loss")
        axs[0, 1].set_title("Loss Curves")
        axs[0, 1].legend()
        
        # --- Subplot 3: Entropy & Action Distribution ---
        if 1 in all_metrics:
            action_counts_1 = all_metrics[1].get("action_counts", {0:0, 1:0, 2:0, 3:0, 4:0, 5:0, 6:0})
            actions = sorted(action_counts_1.keys())
            counts = [action_counts_1[a] for a in actions]
            axs[1, 0].bar(actions, counts, color="orange", alpha=0.7)
            axs[1, 0].set_xlabel("Action")
            axs[1, 0].set_ylabel("Frequency")
            axs[1, 0].set_title("Agent 1 Action Distribution")
        
        # Subplot (1,1): Action Distribution for Agent 2
        if 2 in all_metrics:
            action_counts_2 = all_metrics[2].get("action_counts", {0:0, 1:0, 2:0, 3:0, 4:0, 5:0, 6:0})
            actions = sorted(action_counts_2.keys())
            counts = [action_counts_2[a] for a in actions]
            axs[1, 1].bar(actions, counts, color="green", alpha=0.7)
            axs[1, 1].set_xlabel("Action")
            axs[1, 1].set_ylabel("Frequency")
            axs[1, 1].set_title("Agent 2 Action Distribution")
        
        fig.tight_layout(rect=[0, 0, 1, 0.95])
        plt.suptitle(f"Agents Training Metrics", fontsize=16)
        plt.draw()
        plt.pause(update_interval)
