#include <iostream>
#include <memory>
#include <string>
#include <grpcpp/grpcpp.h>
#include "hello.grpc.pb.h"

using grpc::Channel;
using grpc::ClientContext;
using grpc::ClientReader;
using grpc::Status;
using helloworld::HelloRequest;
using helloworld::HelloReply;
using helloworld::HelloEvent;
using helloworld::Empty;
using helloworld::Greeter;

class GreeterProxy 
{
    private:
        std::unique_ptr<Greeter::Stub> stub;

    public:
        GreeterProxy(std::shared_ptr<Channel> channel) : stub(Greeter::NewStub(channel))
        {            
        }

        std::string SayHello(const std::string& user) 
        {
            HelloRequest request;
            request.set_name(user);

            HelloReply reply;
            ClientContext context;
            Status status = stub->SayHello(&context, request, &reply);

            if (!status.ok()) 
            {
                std::cout << status.error_code() << ": " << status.error_message() << std::endl;
                return "RPC failed";
            }

            return reply.message();
        }

        void ListEvents() 
        {
            Empty empty;
            ClientContext context;
            std::unique_ptr<ClientReader<HelloEvent>> reader(stub->Subscribe(&context, empty));
            
            HelloEvent helloEvent;
            while (reader->Read(&helloEvent)) 
            {
                std::cout << helloEvent.message() << std::endl;
            }
            
            Status status = reader->Finish();
            if (!status.ok()) 
            {
                std::cout << status.error_code() << ": " << status.error_message() << std::endl;
                return;
            }
        }
};

int main(int argc, char** argv) 
{
    // Check usage
    if (argc != 2) {
        std::cerr << "Usage: " << argv[0] << " hostname:port" << std::endl;
        return 1;
    }

    // Create the channel and proxy
    std::shared_ptr<Channel> channel = grpc::CreateChannel(argv[1], grpc::InsecureChannelCredentials());
    GreeterProxy greeter(channel);
    
    // Say hello
    std::string user("Ramon de Klein");
    std::string reply = greeter.SayHello(user);
    std::cout << "Greeter received: " << reply << std::endl;

    // List all events
    greeter.ListEvents();
    return 0;
}
