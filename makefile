export COMPOSE_FILE ?= docker-compose.yml

IMAGE_NAME ?= dauer

build:
	docker-compose build --pull ${args} ${IMAGE_NAME}

up:
	docker-compose up --no-build --remove-orphans --detach ${IMAGE_NAME}

down:
	docker-compose down

stop:
	docker-compose stop

downvolumes:
	docker-compose down -v

downimages:
	docker-compose down --rmi all
	
logs:
	docker-compose logs -f ${IMAGE_NAME}

status:
	docker-compose ps

push:
	docker-compose push

deploy:
	docker stack deploy --compose-file ${COMPOSE_FILE} dauer

servicelogs:
	docker service logs dauer_dauer --no-trunc -f

bash:
	docker exec -it dauer_dauer_1 /bin/bash

psql:
	docker exec -it dauer_postgres_1 su -c psql postgres

clean:
	docker stack rm dauer
	docker stack rm registry
