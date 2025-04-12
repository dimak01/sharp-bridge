# Sharp Bridge

A .NET/C# bridge application for connecting iPhone's VTube Studio to PC. This tool receives tracking data from iPhone, processes it according to user-defined configurations, and sends it to VTube Studio on PC.

This project is inspired by and references [rusty-bridge](https://github.com/ovROG/rusty-bridge), a similar tool implemented in Rust.

## Features (Planned)

- Receive tracking data from iPhone VTube Studio app via UDP
- Transform tracking data using customizable parameter mappings
- Send processed data to VTube Studio on PC via WebSocket
- Configuration system for custom parameter transformations
- Command-line interface for basic operations
- (Future) Enhanced UI interface for easier configuration and monitoring

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).

## Acknowledgments

- [ovROG/rusty-bridge](https://github.com/ovROG/rusty-bridge) - The original Rust implementation that inspired this project 