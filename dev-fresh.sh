#!/usr/bin/env bash
# Like ./dev.sh, but starts from nothing every time: deletes the dev database, sign-in keys and backups, then
# runs the app with hot reload. Use it to go through first-run setup again. An installed ChefBot is not touched.
exec "$(dirname "${BASH_SOURCE[0]}")/dev.sh" fresh "$@"
