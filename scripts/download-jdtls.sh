#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
TARGET_DIR="$REPO_ROOT/.tools/jdtls"
DOWNLOAD_URL="https://download.eclipse.org/jdtls/snapshots/jdt-language-server-latest.tar.gz"

if compgen -G "$TARGET_DIR/plugins/org.eclipse.equinox.launcher_*.jar" >/dev/null; then
  echo "jdtls already present at $TARGET_DIR, skipping download."
else
  echo "Downloading jdtls to $TARGET_DIR..."
  mkdir -p "$TARGET_DIR"
  ARCHIVE="$(mktemp)"
  curl -sL -o "$ARCHIVE" "$DOWNLOAD_URL"
  tar -xzf "$ARCHIVE" -C "$TARGET_DIR"
  rm -f "$ARCHIVE"
  echo "jdtls extracted."
fi

LAUNCHER_JAR="$(compgen -G "$TARGET_DIR/plugins/org.eclipse.equinox.launcher_*.jar" | head -n 1)"

case "$(uname -s)" in
  Linux*) CONFIG_SUBDIR="config_linux" ;;
  Darwin*) CONFIG_SUBDIR="config_mac" ;;
  MINGW*|MSYS*|CYGWIN*) CONFIG_SUBDIR="config_win" ;;
  *) CONFIG_SUBDIR="config_linux" ;;
esac

echo
echo "jdtls requires Java 21+ just to run itself — this is separate from the JDK used to"
echo "analyze the code users write (which should match the Judge0 execution JDK, e.g. 17)."
echo
echo "Configure StackDuel.Api user secrets:"
echo "  dotnet user-secrets set --project $REPO_ROOT/StackDuel.Api LanguageServer:Enabled true"
echo "  dotnet user-secrets set --project $REPO_ROOT/StackDuel.Api LanguageServer:Java:JdtlsLauncherJarPath \"$LAUNCHER_JAR\""
echo "  dotnet user-secrets set --project $REPO_ROOT/StackDuel.Api LanguageServer:Java:JdtlsConfigDirectory \"$TARGET_DIR/$CONFIG_SUBDIR\""
echo "  dotnet user-secrets set --project $REPO_ROOT/StackDuel.Api LanguageServer:Java:JdtlsRuntimeJavaHome \"<path to a JDK 21+ install>\""
echo "  dotnet user-secrets set --project $REPO_ROOT/StackDuel.Api LanguageServer:Java:JavaHome \"<path to the JDK used for user-code analysis, e.g. the same JDK 17 Judge0 compiles with>\""
