#!/bin/bash
set -e

VERSION=$1
if [ -z "$VERSION" ]; then
    echo "Usage: ./build-package.sh <version>"
    echo "Example: ./build-package.sh 1.0.1"
    exit 1
fi

echo "🔨 Building TestRunner v$VERSION"

# Update version in .csproj
sed -i "s/<Version>.*<\/Version>/<Version>$VERSION<\/Version>/" TestRunner/TestRunner.csproj

# Clean previous builds
rm -rf TestRunner/nupkg

# Build package
cd TestRunner
dotnet pack -c Release

cd ..

echo ""
echo "✅ Package created: TestRunner/nupkg/TestRunner.$VERSION.nupkg"
echo "✅ Symbols package: TestRunner/nupkg/TestRunner.$VERSION.snupkg"
echo ""
echo "📦 Package contents:"
echo "   - testrunner CLI tool"
echo "   - TestRunner libraries"
echo "   - README.md"
echo "   - Symbol files for debugging"
echo ""
echo "🧪 To test locally:"
echo "   dotnet tool install --global --add-source ./TestRunner/nupkg TestRunner"
echo "   testrunner --help"
echo ""
echo "📤 To publish to NuGet.org:"
echo "   dotnet nuget push TestRunner/nupkg/TestRunner.$VERSION.nupkg \\"
echo "       --api-key YOUR_API_KEY \\"
echo "       --source https://api.nuget.org/v3/index.json"
echo ""
echo "🔐 To publish to GitHub Packages:"
echo "   dotnet nuget push TestRunner/nupkg/TestRunner.$VERSION.nupkg \\"
echo "       --api-key YOUR_GITHUB_TOKEN \\"
echo "       --source https://nuget.pkg.github.com/piera23/index.json"
