#!/bin/bash

# Clear existing settings
rm -f ~/.config/BrowserRedirector/settings.json

# Run the app, wait for 3 seconds, then kill it
echo "Starting first instance... (will close automatically)"
(cd /home/stevenlafl/Projects/defaultbrowser/DefaultBrowser && dotnet run) &
APP_PID=$!
sleep 3
kill $APP_PID 2>/dev/null

# Check if settings file exists
echo ""
echo "Checking if settings.json was created:"
ls -la ~/.config/BrowserRedirector/

# Show file contents if it exists
if [ -f ~/.config/BrowserRedirector/settings.json ]; then
    echo ""
    echo "Contents of settings.json:"
    cat ~/.config/BrowserRedirector/settings.json
else
    echo ""
    echo "ERROR: settings.json was not created!"
fi

echo ""
echo "Check logs for more information:"
tail -20 ~/.config/BrowserRedirector/log*.txt