DAUER_CONTAINER_ID="$(docker ps -aqf name=dauer_dauer_1)"
echo Writing LetsEncrypt data to dauer_dauer_1 $DAUER_CONTAINER_ID

docker cp letsencrypt/FluffySpoonAspNetLetsEncryptCertificate_Account $DAUER_CONTAINER_ID:/app 
docker cp letsencrypt/FluffySpoonAspNetLetsEncryptCertificate_Site $DAUER_CONTAINER_ID:/app 
docker cp letsencrypt/FluffySpoonAspNetLetsEncryptChallenge_Challenges $DAUER_CONTAINER_ID:/app