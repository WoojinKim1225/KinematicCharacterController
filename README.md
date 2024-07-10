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



## Installation

[Explain installation process]

## Usage

[Provide basic usage instructions]

## Customization Guide

[Brief guide on how to customize the controller]

## Future Development Plans

- [ ] Implement Dynamic Rigidbody methods:
  - [ ] AddForce()
  - [ ] AddTorque()
  - [ ] Other related methods...
- [ ] Implement additional constraints:
  - [ ] Slope movement restrictions
  - [ ] Maximum velocity limits
  - [ ] Others...
- [ ] Add advanced movement features:
  - [ ] Wall running
  - [ ] Double jump
  - [ ] Air dash
- [ ] Improve physics-based interactions:
  - [ ] Push/pull functionality
  - [ ] Enhanced object collision responses
- [ ] Performance optimization
- [ ] Improve documentation and create API reference
- [ ] Create sample scenes and demo projects

## Contributing

If you'd like to contribute to this project, please feel free to submit a Pull Request. All contributions are welcome!

## License

[Include license information]
