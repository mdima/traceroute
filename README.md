# Visual Trace Route
C# ASP.NET Core 8.0 Visual Trace Route Web Application
![Visual Trace Route Screenshot](screenshot.jpg)

Based on the original repository: https://github.com/bencorn/traceroute

### Improvements from the original repository:
* Project converted to NetCore8
* Changed the maps implemented from Google Maps to OpenStreetMap
* Changed the IP information source from keycdn.com to ip-api.com
* Added a sidebar with the hops information
* Introduced the Unit Tests (Code coverage: > 80%)

### Running in Docker
You can use the following image to run Visual Trace Route locally:
michele73/traceroute:latest

Example:
docker run -d -p 8081:80 --name=traceroute --restart=always -v traecroute_logs:/app/logs michele73/traceroute:latest

### Live Demo
You can view a live demo of the Trace Route application here: https://traceroute.di-maria.it/
