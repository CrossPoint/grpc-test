CXX = g++
CPPFLAGS += `pkg-config --cflags protobuf grpc`
CXXFLAGS += -std=c++11
LDFLAGS += -L/usr/local/lib `pkg-config --libs protobuf grpc++ grpc` -Wl,--no-as-needed -lgrpc++_reflection -Wl,--as-needed -ldl -static
PROTOC = protoc
GRPC_CPP_PLUGIN = grpc_cpp_plugin
GRPC_CPP_PLUGIN_PATH ?= `which $(GRPC_CPP_PLUGIN)`

all: CcClient #CcServer

CcClient: hello.pb.o hello.grpc.pb.o CcClient.o
	$(CXX) $^ $(LDFLAGS) -o $@

#CcServer: hello.pb.o hello.grpc.pb.o CcServer.o
#	$(CXX) $^ $(LDFLAGS) -o $@

%.grpc.pb.cc: %.proto
	$(PROTOC) --grpc_out=. --plugin=protoc-gen-grpc=$(GRPC_CPP_PLUGIN_PATH) $<

%.pb.cc: %.proto
	$(PROTOC) --cpp_out=. $<

clean:
	rm -f *.o *.pb.cc *.pb.h CcClient CcServer
