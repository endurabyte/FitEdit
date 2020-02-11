docker swarm init
docker service create --name registry --publish published=5000,target=5000 registry:2
docker service create --replicas 2 --name fitapp 127.0.0.1:5000/fitapp:latest
make build (does docker service create)
docker-compose push
docker stack deploy --compose-file docker-compose.yml fitapp
docker stack services fitapp
docker service ls
docker stack ls
docker node ls
docker service logs fitapp_fitapp --no-trunc

docker stack rm fitapp
docker stack rm registry
docker swarm leave --force