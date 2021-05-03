# WildfireSimulator

The repository for this project is split into 3 components:
1. data
2. simulation
3. analysis

The `data` component contains the project for the backend of this system. This includes all data and flask applications needed to run the simulation.
The `simulation` component contains the Unity Project in which the simulation is run.
The `analysis` component contains the Matlab code used to run the polygon analysis on the results of the simulation.

### Data
To run the data component. The user must create a new python virtual environment and install all of the requirements from the `requirements.txt`, the large file `40_scott_and_burgan_classified.tif` file must be placed inside of the 'data/src/fuel_model/data' directory, and both theflask applications must be started using the following commands:

  ##### fuel_model/app.py
  `flask run -p 5000`
  
  ##### fuel_moisture/app.py
  `flask run -p 5500`
  
### Simulation
To run the simulation, the project `simulation.sln` must be loaded as a unity project. Once the flask applications have booted up, the unity project can be run inside of the unity editor.

### Analysis
To run the analysis, the demo.m file is used.
