cmake_minimum_required(VERSION 3.20)
project(f2c_sidecar LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(Protobuf CONFIG REQUIRED)
find_package(gRPC CONFIG REQUIRED)

set(PROTO_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../proto)
set(PROTO_FILE aog/guidance/v1/planner.proto)

protobuf_generate_cpp(PROTO_SRCS PROTO_HDRS ${PROTO_DIR}/${PROTO_FILE})
grpc_generate_cpp(GRPC_SRCS GRPC_HDRS ${PROTO_DIR}/${PROTO_FILE})

add_executable(f2c-sidecar
    src/main.cc
    ${PROTO_SRCS}
    ${PROTO_HDRS}
    ${GRPC_SRCS}
    ${GRPC_HDRS})

target_link_libraries(f2c-sidecar
    PRIVATE
        protobuf::libprotobuf
        gRPC::grpc++
)

target_include_directories(f2c-sidecar
    PRIVATE
        ${CMAKE_CURRENT_BINARY_DIR}
        ${CMAKE_CURRENT_BINARY_DIR}/../proto
)

install(TARGETS f2c-sidecar RUNTIME DESTINATION bin)
