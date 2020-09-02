#!/bin/bash

if [[ -f ".env_private" && -f "docker-compose.yml_private" && -f "kubernetes/todomvcui-deployment.yaml_private" && -f "kubernetes/todoapi-deployment.yaml_private" ]]; then
       echo "Repo already public."
else
	echo "Repo private, changing to public."
        # Transition from private .env file to public .env file
	echo "Transition from private .env file to public .env file"
	mv .env .env_private
	mv .env_public .env
	# Transition from private docker-compose.yml file to public docker-compose.yml file
	echo "Transition from private docker-compose.yml file to public docker-compose.yml file"
	mv docker-compose.yml docker-compose.yml_private
	mv docker-compose.yml_public docker-compose.yml
	# Transition private kubernetes specs to public kubernetes specs
	echo "Transition private kubernetes specs to public kubernetes specs"
	mv kubernetes/todomvcui-deployment.yaml kubernetes/todomvcui-deployment.yaml_private
	mv kubernetes/todomvcui-deployment.yaml_public kubernetes/todomvcui-deployment.yaml
	mv kubernetes/todoapi-deployment.yaml kubernetes/todoapi-deployment.yaml_private
	mv kubernetes/todoapi-deployment.yaml_public kubernetes/todoapi-deployment.yaml
fi

echo "Done!"

exit 0
