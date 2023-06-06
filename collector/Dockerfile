FROM public.ecr.aws/aws-observability/aws-otel-collector:latest
COPY collector-config.yaml /etc/ecs/otel-config.yaml
CMD ["--config=/etc/ecs/otel-config.yaml"]
