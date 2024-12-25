# Visual Trace Route
C# ASP.NET Core 9.0 Visual Trace Route Web Application
![Visual Trace Route Screenshot](https://github.com/mdima/traceroute/blob/master/SupportFiles/screenshot.png?raw=true)

Based on the original repository: https://github.com/bencorn/traceroute

Running this container you will join the Visual Traceroute network and will allow you to run traceroutes from different sources and 
at the same time will offer your host as source for other users. You can disable this feature by setting the environment variable (see below).

### Improvements from the original repository:
* Project converted to NetCore9
* Changed the maps implemented from Google Maps to OpenStreetMap
* Changed the IP information source from keycdn.com to ip-api.com
* Added a sidebar with the hops information
* Introduced the Unit Tests (Code coverage: > 80%)
* Many interface improvements
* Added a security check on all the inbound parameters to avoid command injection
* Added the multi-source traceroute feature to allow to run the traceroute from different sources

### Running in Docker
You can use the following image to run Visual Trace Route locally:
michele73/traceroute:2.0.2

Example:
```
docker run -d -p 8081:80 --name=traceroute --restart=always -v traecroute_logs:/app/logs michele73/traceroute:2.0.2
```

The image repository is here: https://hub.docker.com/r/michele73/traceroute

### Environment variables
* TRACEROUTE_ENABLEREMOTETRACES (default true): Allows the user to choose the source of the traceroute. 
* TRACEROUTE_HOSTREMOTETRACES (default true): Allows the host to be used as a source for the traceroute from other users.

### Live Demo
You can view a live demo of the Trace Route application here: https://traceroute.di-maria.it/
