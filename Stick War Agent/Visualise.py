

import matplotlib.pyplot as plt
import matplotlib.ticker as ticker
import queue

import os



def live_plot_all_metrics(all_metrics, update_interval=5):
    """
    Live plot function to update a 2x2 grid of subplots with training metrics.
    
    Parameters:
      all_metrics: A dictionary with agent IDs as keys and their metrics as values.
      update_interval: How frequently (in seconds) to update the plot.
    """
    os.makedirs("graphs", exist_ok=True)
    plt.ion()  # Turn on interactive mode
    fig, axs = plt.subplots(2, 2, figsize=(14, 10))
    
    while True:
        
        # Clear each subplot before re-plotting
        axs[0, 0].clear()
        axs[0, 1].clear()
        axs[1, 0].clear()
        axs[1, 1].clear()

        axs[0, 0].xaxis.set_major_locator(ticker.MaxNLocator(integer=True))
        axs[0, 1].xaxis.set_major_locator(ticker.MaxNLocator(integer=True))
        
        # Subplot 1: Total Reward per Episode for all agents
        for agent_id, metrics in all_metrics.items():
            episode_rewards = metrics.get("episode_rewards", [])
            episodes = metrics.get("episodes", list(range(1, len(episode_rewards)+1)))
            axs[0, 0].plot(episodes, episode_rewards, marker='o', linestyle='-', label=f"Agent {agent_id}")
        axs[0, 0].set_xlabel("Episode")
        axs[0, 0].set_ylabel("Total Reward")
        axs[0, 0].set_title("Agents Total Reward")
        axs[0, 0].legend(fontsize = 16)

        # Subplot 2: Loss Curves for all agents
        for agent_id, metrics in all_metrics.items():
            episodes = metrics.get("episodes", [])
            policy_losses = metrics.get("policy_losses", [])
            value_losses = metrics.get("value_losses", [])
            total_losses = metrics.get("total_losses", [])
            if episodes:
                axs[0, 1].plot(episodes, policy_losses, label=f"Policy Loss Agent {agent_id}")
                axs[0, 1].plot(episodes, value_losses, label=f"Value Loss Agent {agent_id}")
                axs[0, 1].plot(episodes, total_losses, label=f"Total Loss Agent {agent_id}")
        axs[0, 1].set_xlabel("Episode")
        axs[0, 1].set_ylabel("Loss")
        axs[0, 1].set_title("Loss Curves")
        axs[0, 1].legend(fontsize = 16)
        
        # Subplot 3: Action Distribution for Agent 1 (if available)
        if "1" in all_metrics:
            action_counts_1 = all_metrics["1"].get("action_counts", {0:0, 1:0, 2:0, 3:0, 4:0, 5:0, 6:0})
            actions = sorted(action_counts_1.keys())
            counts = [action_counts_1[a] for a in actions]
            axs[1, 0].bar(actions, counts, color="orange", alpha=0.7)
            axs[1, 0].set_xlabel("Action")
            axs[1, 0].set_ylabel("Frequency")
            axs[1, 0].set_title("Agent 1 Action Distribution")
        
        # Subplot 4: Action Distribution for Agent 2 (if available)
        if "2" in all_metrics:
            action_counts_2 = all_metrics["2"].get("action_counts", {0:0, 1:0, 2:0, 3:0, 4:0, 5:0, 6:0})
            actions = sorted(action_counts_2.keys())
            counts = [action_counts_2[a] for a in actions]
            axs[1, 1].bar(actions, counts, color="green", alpha=0.7)
            axs[1, 1].set_xlabel("Action")
            axs[1, 1].set_ylabel("Frequency")
            axs[1, 1].set_title("Agent 2 Action Distribution")
        
        fig.tight_layout(rect=[0, 0, 1, 0.95])
        plt.suptitle("Agents Training Metrics", fontsize=16)
        plt.draw()
        plt.pause(update_interval)
