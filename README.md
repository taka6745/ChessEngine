# Tom Kerrigan's Simple Chess Program (TSCP) for macOS and ESP32

## Overview
This project is an adaptation of Tom Kerrigan's Simple Chess Program (TSCP), originally created in 1997. The aim is to make TSCP compatible with macOS and to extend its functionality for parallel execution on the ESP32 microcontroller. This adaptation allows TSCP to interface with a physical chessboard, providing a unique blend of classic chess programming and modern hardware interaction.

### Original TSCP
TSCP is a renowned educational tool for those interested in chess programming. Its straightforward and well-documented source code makes it an excellent starting point for understanding chess algorithms and AI design.

### Project Goals
1. **macOS Compatibility:** Modify TSCP to run seamlessly on macOS, ensuring all functionalities are preserved and optimized for the platform.
2. **ESP32 Integration:** Parallelize TSCP to run on an ESP32 microcontroller. This involves adapting the codebase for efficient execution on the ESP32's dual-core processor and enabling communication with a physical chessboard setup.
3. **Physical Board Interaction:** Develop a system where the chess engine's moves are represented on a physical board, providing an interactive chess-playing experience.

## Running TSCP on macOS
To run TSCP on macOS:

1. Clone the repository:
   ```
   git clone [repository-url]
   ```

2. Navigate to the cloned directory and compile the source code:
   ```
   cd [cloned-directory]
   gcc -o tscp tscp.c
   ```

3. Run the executable:
   ```
   ./tscp
   ```

## Adaptation for ESP32
The adaptation of TSCP for ESP32 involves parallelizing the code to leverage the dual-core architecture of the ESP32 microcontroller. This will improve the engine's efficiency and responsiveness, especially when interfacing with physical chessboard components. The integration aims to create a real-time chess-playing experience, where the engine's moves are displayed on an electronic chessboard.

### Planned Features:
- **Parallel Processing:** Utilizing the ESP32's processing capabilities to run chess algorithms efficiently.
- **Physical Board Interface:** Developing hardware and software interfaces to represent the game state on a physical chessboard.
- **Real-Time Interaction:** Enabling real-time gameplay, with the ESP32 responding to player moves detected by the physical board.

### Running TSCP on ESP32
Instructions for deploying and running TSCP on ESP32 will be provided as the project progresses.

