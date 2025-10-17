#include <chrono>
#include <iostream>
#include <memory>
#include <string>

#include <grpcpp/grpcpp.h>

#include "aog/guidance/v1/planner.grpc.pb.h"

using grpc::Server;
using grpc::ServerBuilder;
using grpc::ServerContext;
using grpc::Status;

namespace aog::guidance::v1 {

class PlannerServiceImpl final : public Planner::Service {
 public:
  Status MakePlan(ServerContext* context, const PlanRequest* request,
                  PlanResponse* response) override {
    (void)context;
    std::cerr << "Received MakePlan placeholder request for field_rev="
              << request->field_rev() << std::endl;
    response->set_status(PlanStatus::PLAN_STATUS_UNIMPLEMENTED);
    response->set_message("Planner sidecar stub not yet implemented");
    return Status::OK;
  }

  Status HealthCheck(ServerContext* context, const HealthCheckRequest* request,
                     HealthCheckResponse* response) override {
    (void)context;
    response->set_status(HealthCheckResponse::SERVING);
    response->set_f2c_version("stub");
    response->set_f2c_license("TBD");
    if (request->include_timestamp()) {
      auto now = std::chrono::system_clock::now();
      auto secs = std::chrono::time_point_cast<std::chrono::seconds>(now);
      response->set_timestamp_utc(std::to_string(secs.time_since_epoch().count()));
    }
    return Status::OK;
  }
};

}  // namespace aog::guidance::v1

namespace {

void RunServer(const std::string& listen_addr) {
  aog::guidance::v1::PlannerServiceImpl service;

  ServerBuilder builder;
  builder.AddListeningPort(listen_addr, grpc::InsecureServerCredentials());
  builder.RegisterService(&service);

  std::unique_ptr<Server> server(builder.BuildAndStart());
  if (!server) {
    throw std::runtime_error("Failed to start planner sidecar server");
  }

  std::cout << "f2c-sidecar listening on " << listen_addr << std::endl;
  server->Wait();
}

}  // namespace

int main(int argc, char** argv) {
  std::string listen_addr = "0.0.0.0:43051";
  if (argc > 1) {
    listen_addr = argv[1];
  }

  try {
    RunServer(listen_addr);
  } catch (const std::exception& ex) {
    std::cerr << "Planner sidecar failed: " << ex.what() << std::endl;
    return 1;
  }
  return 0;
}
