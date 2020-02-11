export COMPOSE_FILE ?= docker-compose.yml

IMAGE_NAME ?= fitapp

build:
	docker-compose build --pull ${args} ${IMAGE_NAME}

up:
	docker-compose up --no-build --remove-orphans --detach ${IMAGE_NAME}

down:
	docker-compose down

push:
	docker-compose push

deploy:
	docker stack deploy --compose-file ${COMPOSE_FILE} fitapp

servicelogs:
	docker service logs fitapp_fitapp --no-trunc -f

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

clean:
	docker stack rm fitapp
	docker stack rm registry
