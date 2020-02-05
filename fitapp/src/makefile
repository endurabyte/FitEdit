export COMPOSE_FILE ?= docker-compose.yml

IMAGE_NAME ?= fitapp

build:
	docker-compose build --pull ${args} ${IMAGE_NAME}

run:
	docker-compose up --no-build --detach ${IMAGE_NAME}

logs:
	docker-compose logs -f ${IMAGE_NAME}

bash:
	docker exec -it src_fitapp_1 /bin/bash

psql:
	docker exec -it src_postgres_1 su -c psql postgres

stop:
	docker-compose stop

down:
	docker-compose down -v

status:
	docker-compose ps
