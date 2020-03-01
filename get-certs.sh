DAUER_CONTAINER_ID="$(docker ps -aqf name=dauer_dauer_1)"
echo Fetching LetsEncrypt data from dauer_dauer_1 $DAUER_CONTAINER_ID

mkdir -p letsencrypt
docker cp $DAUER_CONTAINER_ID:/app/FluffySpoonAspNetLetsEncryptCertificate_Account letsencrypt/
docker cp $DAUER_CONTAINER_ID:/app/FluffySpoonAspNetLetsEncryptCertificate_Site letsencrypt/
docker cp $DAUER_CONTAINER_ID:/app/FluffySpoonAspNetLetsEncryptChallenge_Challenges letsencrypt/
