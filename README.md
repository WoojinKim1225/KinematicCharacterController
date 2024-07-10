# Unity Customizable Kinematic Character Controller(WIP)

This project is an extensible and customizable kinematic character controller solution inspired by [pokeblokdude's character-controller](https://github.com/pokeblokdude/character-controller).

## Project Goals

The main objectives of this project are:

1. Provide a high level of customization
2. Implement Dynamic Rigidbody functionalities in a Kinematic Character Controller
3. Support various constraints
4. Offer an extensible character movement asset

## Key Features

My character collider currently supports these features:

1. Normal features:
- Custom collision detection
- Ground, Wall, Celling detection
- Smooth movement on slopes
- Climb and desending steps
- Jump height and jump velocity
- Crouch and Run

2. Extensible features
- Velocity-dependent multi-stage gravity
- Multi(Air) Jump
- Changing player capsule up direction
- Changing gravity direction
- Jump buffer and coyote time


3. Dynamic features
- AddForce(Force, Impulse, Acceleration, ChangeVelocity) Method
- AddRelativeForce(Force, Impulse, Acceleration, ChangeVelocity) Method
- Move, MovePosition, MoveRotation Method
- SetVelocity Method

You can make your character Swim, Fly, Dash, Wall-Run with these methods.



## Installation

To install this Kinematic Character Controller in your Unity project:

1. Go to the [Releases](https://github.com/WoojinKim1225/KinematicCharacterController/releases) page of this repository.
2. Download the latest `.unitypackage` file from the most recent release.
3. Open your Unity project.
4. In Unity, go to Assets > Import Package > Custom Package.
5. Select the downloaded `.unitypackage` file.
6. In the Import Unity Package window, ensure all items are selected and click 'Import'.

After importing, the character controller assets will be available in your project.


## Future Development Plans

- [ ] Implement Dynamic Rigidbody methods:
  - [ ] AddTorque()
  - [ ] Rotate()
  - [ ] Other related methods...
- [ ] Implement additional constraints:
  - [ ] Distance constraint
  - [ ] Maximum velocity constraint
  - [ ] Spring constraint
  - [ ] Joint constraint
  - [ ] Other related constraints...
- [ ] Improve physics-based interactions:
  - [ ] Push/pull functionality
  - [ ] Enhanced object collision responses
- [ ] Performance optimization
- [ ] Improve documentation and create API reference
- [ ] Create sample scenes and demo projects
