docker build -f .\Dockerfile-netcore -t ramondeklein/grpc-client-dotnet:latest --target client .                                    
docker build -f .\Dockerfile-netcore -t ramondeklein/grpc-server-dotnet:latest --target server .                                    
docker build -f .\Dockerfile-cc -t ramondeklein/grpc-client-cc:latest --target client .
