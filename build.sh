#!/bin/bash

SOLUTION="SSD1306CLI.sln"
BUILD_CONF="Release"

nuget restore $SOLUTION
msbuild -property:Configuration=$BUILD_CONF -property:GitCommit=$(git rev-parse HEAD) $SOLUTION
