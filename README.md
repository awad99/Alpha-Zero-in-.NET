# AlphaZero Chess in .NET

A premium, high-performance C# Windows Forms implementation of the **AlphaZero** reinforcement learning algorithm applied to the game of **Chess**. This project features an interactive visual board and is architected to support self-play training, neural-guided search, and real-time visualization of the AI's decision-making process.

---

## Current Features
- **Adaptive Full-Screen Layout**: The chessboard automatically scales using a `TableLayoutPanel` to fill the entire screen dynamically when the application is launched.
- **Modern Visual Styling**: Clean dark-mode aesthetic with custom-tailored light/dark squares and smooth hover transitions.
- **Interactive Coordinates Feedback**: Click on any square on the board to display its precise chess algebraic notation (e.g., `e4`, `f7`) via an elegant ToolTip.

---

## Planned Features (Coming Soon)

### 1. Lightweight Chess Game Engine (`ChessEngine`)
- **Move Generator**: Full implementation of chess rules including valid moves for all pieces (Pawns, Knights, Bishops, Rooks, Queens, and Kings).
- **Special Moves**: Support for castling, en-passant, and pawn promotions.
- **State Evaluator**: Automatic checkmate, stalemate, and draw condition detection.

### 2. Dual-Headed Custom Neural Network (`ChessNeuralNetwork`)
- **Pure C# Implementation**: Built from scratch using modern C# optimizations (supporting SIMD / `System.Numerics`) to run efficiently on .NET Framework 4.7.2 without heavy external dependencies like PyTorch or TensorFlow.
- **Policy Head**: Predicts probability distributions across all potential moves, modeled as a source-destination action space ($64 \times 64 = 4096$ possible moves).
- **Value Head**: Outputs a scalar evaluation $v \in [-1, 1]$ representing the expected outcome of the game from the current player's perspective.
- **Backpropagation & Training Loop**: Standard gradient descent optimization with L2 regularization to minimize policy and value errors.

### 3. Neural-Guided Monte Carlo Tree Search (`ChessMCTS`)
- **PUCT Algorithm**: Balances exploration of unvisited moves and exploitation of high-value nodes guided by the neural network's policy probabilities.
- **Expansion & Backpropagation**: Evaluates leaf nodes using the neural network and backpropagates outcomes to update search statistics.

### 4. Self-Play Training Pipeline (`AlphaZeroTrainer`)
- **Laying the Replay Buffer**: AI plays thousands of games against itself, logging the board state, MCTS search policy $\pi$, and final game outcome $z \in \{-1, 0, 1\}$.
- **Reinforcement Training**: Batches are sampled from the replay buffer to continuously update and optimize the neural network weights.

### 5. Training & Analytics Console (UI)
- **AI Game Controls**: Adjust training parameters directly from the sidebar (number of self-play games, MCTS simulations, learning rate, temperature).
- **Live Loss Tracking**: Real-time graphing showing policy and value loss convergence as the AI gains chess proficiency.
- **MCTS Thought Heatmap**: Visual overlay displaying which squares the AI is considering and its confidence levels in real-time.

---

## How to Build and Run

### Prerequisites
- Windows OS
- Visual Studio 2022 (Community, Professional, or Enterprise)
- .NET Framework 4.7.2 SDK

### Compilation
1. Open the solution in **Visual Studio 2022**.
2. Press **F5** or click the green **Start** button in the toolbar to run the interactive screen.
3. Alternatively, compile from the command line using MSBuild:
   ```bash
   msbuild AlphaZero.csproj /t:Build /p:Configuration=Debug
   ```
