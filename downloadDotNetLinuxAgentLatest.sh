#!/bin/sh

#######################################################################
# This script will fetch the latest AppDynamics Dotnet Core agent     #
# and place it in the current working directory.                      #
# This script requires:                                               #
# curl                                                                #
# unzip       							      #
# jq 								      #
#######################################################################

# Download Root URL
DOWNLOAD_PATH=https://download-files.appdynamics.com/

# Fetch latest Sun Java Agent download path from AppD
FILE_PATH=$(curl https://download.appdynamics.com/download/downloadfilelatest/ | jq -r '.[].s3_path' | grep DotNetCore-linux-x64)

# Construct the full URL
DOWNLOAD_PATH=$DOWNLOAD_PATH$FILE_PATH

# Print URL to stdout
echo "Downloading agent from: " $DOWNLOAD_PATH

# Fetch agent and write into working directory
curl -L $DOWNLOAD_PATH -o ./DotNetCore-linux-x64.zip

# Unzip the agent in current working directory
unzip -u ./DotNetCore-linux-x64.zip -x AppDynamicsConfig.json.template README.md
# Remove the zip
rm -f ./DotNetCore-linux-x64.zip
