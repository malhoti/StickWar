

import matplotlib.pyplot as plt
import matplotlib.ticker as ticker
import queue

import os

import matplotlib.pyplot as plt
import matplotlib.ticker as ticker
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
        # Clear all subplots.
        for ax in axs.flatten():
            ax.clear()

        # Set x-axis tick locators for the first two subplots.
        axs[0, 0].xaxis.set_major_locator(ticker.MaxNLocator(integer=True))
        axs[0, 1].xaxis.set_major_locator(ticker.MaxNLocator(integer=True))
        
        # For each agent, plot its metrics.
        for agent_id, metrics in all_metrics.items():
            # print(metrics)
            
            # Retrieve metrics for total rewards and episodes.
            mode = metrics.get("mode")
            episode_rewards = metrics.get("episode_rewards", [])
            results = metrics.get("result", [])
            
            # Use the stored episode numbers if available; otherwise, create a default range.
            episodes = metrics.get("episodes", list(range(1, len(episode_rewards)+1)))
            total_losses = metrics.get("total_losses", [])
            
            # min_len = min(len(episodes), len(total_losses), len(episode_rewards))win rate abov
            # episodes = episodes[:min_len]
            # total_losses = total_losses[:min_len]
            # episode_rewards = episode_rewards[:min_len]
            
            # Plot Total Reward and Total Loss for this agent.
            
            axs[0, 0].plot(episodes, episode_rewards, marker='o', linestyle='-', label=f"Agent {agent_id}")
            if mode == "train":
                axs[0, 1].plot(episodes, total_losses, label=f"Total Loss Agent {agent_id}")


            
            
            # Plot action distribution for each agent using its own action_counts.
            action_counts = metrics.get("action_counts", {0: 0, 1: 0, 2: 0, 3: 0, 4: 0, 5: 0, 6: 0})
            actions = sorted(action_counts.keys())
            total_actions = sum(action_counts.values())
            counts = [action_counts[a] / total_actions if total_actions > 0 else 0 for a in actions]
            
            # Decide which subplot to use for the bar plot based on the agent_id.
            if agent_id == "1":
                axs[1, 0].bar(actions, counts, color="orange", alpha=0.7, label=f"Agent {agent_id}")
                axs[1, 0].set_xlabel("Action")
                axs[1, 0].set_ylabel("Normalized Frequency")
                axs[1, 0].set_title("Agent 1 Action Distribution")

            # WINRATEEEE
                last_results = results[-100:] if len(results) >= 100 else results

                if len(results)==0:
                    continue

                wins = sum(1 for r in last_results if r == 0)
                losses = sum(1 for r in last_results if r == 1)
                draws = sum(1 for r in last_results if r == 2)
                total_games = len(last_results)

                win_rate = wins / total_games
                # Create episode numbers for the last results.
                
                
                # Compute win rate over these episodes.
                
                
                # Plot individual episode results (1=win, 0=loss/draw).
                axs[1,1].bar(["Win", "Loss", "Draw"], [wins / total_games, losses / total_games, draws / total_games], color=["green", "red", "gray"])
                axs[1,1].axhline(0.65, color='red', linestyle='--', label="65% Threshold")
                
                
                if win_rate >= 0.65:
                    print(f"Agent {agent_id} win rate above 65%: {win_rate*100:.2f}%")
            
                axs[1,1].set_title(f"Agent {agent_id} Win Rate (Last {len(last_results)} Episodes)")

        # Set labels, titles, and legends for the top two subplots.
        axs[0, 0].set_xlabel("Episode")
        axs[0, 0].set_ylabel("Total Reward")
        axs[0, 0].set_title("Agents Total Reward")
        axs[0, 0].legend(fontsize=16)
        
        axs[0, 1].set_xlabel("Episode")
        axs[0, 1].set_ylabel("Total Loss")
        axs[0, 1].set_title("Total Loss Curves")
        axs[0, 1].legend(fontsize=16)


        axs[1,1].set_xlabel("Episode")
        axs[1,1].set_ylabel("Result (1=Win, 0=Loss/Draw)")
        
        
        # Adjust layout and draw the updated figure.
        fig.tight_layout(rect=[0, 0, 1, 0.95])
        plt.suptitle("Agents Training Metrics", fontsize=16)
        plt.draw()
        plt.pause(update_interval)


# def live_plot_all_metrics(all_metrics, update_interval=5):
#     """
#     Live plot function to update a 2x2 grid of subplots with training metrics.
    
#     Parameters:
#       all_metrics: A dictionary with agent IDs as keys and their metrics as values.
#       update_interval: How frequently (in seconds) to update the plot.
#     """
#     os.makedirs("graphs", exist_ok=True)
#     plt.ion()  # Turn on interactive mode
#     fig, axs = plt.subplots(2, 2, figsize=(14, 10))
    
#     while True:
        
#         # Clear each subplot before re-plotting
#         axs[0, 0].clear()
#         axs[0, 1].clear()
#         axs[1, 0].clear()
#         axs[1, 1].clear()

#         axs[0, 0].xaxis.set_major_locator(ticker.MaxNLocator(integer=True))
#         axs[0, 1].xaxis.set_major_locator(ticker.MaxNLocator(integer=True))

#         action_counts_1 = all_metrics["1"].get("action_counts", {0:0, 1:0, 2:0, 3:0, 4:0, 5:0, 6:0})
#         action_counts_2 = all_metrics["2"].get("action_counts", {0:0, 1:0, 2:0, 3:0, 4:0, 5:0, 6:0})
        
#         # Subplot 1: Total Reward per Episode for all agents
#         for agent_id, metrics in all_metrics.items():
#             episode_rewards = metrics.get("episode_rewards", [])
#             episodes = metrics.get("episodes", list(range(1, len(episode_rewards)+1)))

            
#             # policy_losses = metrics.get("policy_losses", [])
#             # value_losses = metrics.get("value_losses", [])
#             total_losses = metrics.get("total_losses", [])
#             #entropies = metrics.get("entropies", [])

#             axs[0, 0].plot(episodes, episode_rewards, marker='o', linestyle='-', label=f"Agent {agent_id}")

            
#             #axs[0, 1].plot(episodes, policy_losses, label=f"Policy Loss Agent {agent_id}")
#             # axs[0, 1].plot(episodes, value_losses, label=f"Value Loss Agent {agent_id}")
#             axs[0, 1].plot(episodes, total_losses, label=f"Total Loss Agent {agent_id}")
#             #axs[0, 1].plot(episodes, entropies, label=f"Entropy Agent {agent_id}")


            
#             actions = sorted(action_counts_1.keys())
            
#             total_actions = sum(action_counts_1.values())  # Sum of all actions taken
#             if total_actions > 0:
#                 counts = [action_counts_1[a] / total_actions for a in actions]  # Normalize
#             else:
#                 counts = [0] * len(actions)  # If no actions taken, set all to 0

#             axs[1, 0].bar(actions, counts, color="orange", alpha=0.7)


            
#             actions = sorted(action_counts_2.keys())
            
#             total_actions = sum(action_counts_2.values())  # Sum of all actions taken
#             if total_actions > 0:
#                 counts = [action_counts_2[a] / total_actions for a in actions]  # Normalize
#             else:
#                 counts = [0] * len(actions)  # If no actions taken, set all to 0
#             axs[1, 1].bar(actions, counts, color="green", alpha=0.7)


#         axs[0, 0].set_xlabel("Episode")
#         axs[0, 0].set_ylabel("Total Reward")
#         axs[0, 0].set_title("Agents Total Reward")
#         axs[0, 0].legend(fontsize = 16)

        
#         axs[0, 1].set_xlabel("Episode")
#         axs[0, 1].set_ylabel("Total Loss")
#         axs[0, 1].set_title("Total Loss Curves")
#         axs[0, 1].legend(fontsize = 16)
        
       
       
#         axs[1, 0].set_xlabel("Action")
#         axs[1, 0].set_ylabel("Normalized Frequency")
#         axs[1, 0].set_title("Agent 1 Action Distribution")

        
        
#         axs[1, 1].set_xlabel("Action")
#         axs[1, 1].set_ylabel("Normalized Frequency")
#         axs[1, 1].set_title("Agent 2 Action Distribution")

        
#         fig.tight_layout(rect=[0, 0, 1, 0.95])
#         plt.suptitle("Agents Training Metrics", fontsize=16)
#         plt.draw()
#         plt.pause(update_interval)
