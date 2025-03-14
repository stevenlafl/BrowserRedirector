#!/bin/bash

# Define all target runtime identifiers (RIDs)
PLATFORMS=(
  # Windows
  "win-x64"
  "win-x86"
  "win-arm64"
  
  # macOS
  "osx-x64"
  "osx-arm64"
  
  # Linux
  "linux-x64"
  "linux-arm"
  "linux-arm64"
)

# Create output directory
PUBLISH_DIR="./bin/Publish"
mkdir -p "$PUBLISH_DIR"

# Build for each platform
for PLATFORM in "${PLATFORMS[@]}"; do
  echo "Building for $PLATFORM..."
  
  OUTPUT_DIR="$PUBLISH_DIR/$PLATFORM"
  
  # Create self-contained deployment
  dotnet publish -c Release -r "$PLATFORM" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:PublishTrimmed=true \
    -o "$OUTPUT_DIR"
  
  # Create zip file
  if [ -d "$OUTPUT_DIR" ]; then
    ZIP_NAME="BrowserRedirector-$PLATFORM.zip"
    echo "Creating $ZIP_NAME..."
    
    # Check if the platform is Windows and rename the executable to include .exe
    if [[ $PLATFORM == win* ]]; then
      (cd "$OUTPUT_DIR" && zip -r "../../$ZIP_NAME" *)
    else 
      (cd "$OUTPUT_DIR" && zip -r "../../$ZIP_NAME" *)
    fi
  fi
done

echo "Build complete! Output files are in $PUBLISH_DIR and zip files in ./bin/"