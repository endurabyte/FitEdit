docker swarm init
docker service create --name registry --publish published=5000,target=5000 registry:2
make build (does docker service create)
docker-compose push
docker stack deploy --compose-file docker-compose.yml fitapp
docker stack services fitapp
docker service ls
docker stack ls

docker stack rm fitapp
docker stack rm registry
docker swarm leave --force

docker node ls

docker service create --replicas 2 --name fitapp 127.0.0.1:5000/fitapp:latest

docker service logs fitapp --no-trunc