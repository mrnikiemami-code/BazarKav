"""Regression checks for HTTP request DTO placement, using isolated source fixtures."""
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


class HttpRequestPlacementTests(unittest.TestCase):
    def setUp(self):
        self.scratch = tempfile.TemporaryDirectory()
        self.addCleanup(self.scratch.cleanup)
        self.root = Path(self.scratch.name)
        self.guard = self.root / "tools/architecture/check_architecture.py"
        self.guard.parent.mkdir(parents=True)
        shutil.copyfile(Path(__file__).with_name("check_architecture.py"), self.guard)
        for layer in ("Contracts", "Presentation"):
            self.write(f"src/Modules/Demo/{layer}/BazarKav.Demo.{layer}.csproj",
                       '<Project Sdk="Microsoft.NET.Sdk" />')

    def write(self, relative, content):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")

    def run_guard(self):
        return subprocess.run([sys.executable, str(self.guard)], capture_output=True, text=True)

    def test_individual_http_request_in_presentation_is_accepted(self):
        self.write("src/Modules/Demo/Presentation/Requests/CreateDemoRequest.cs",
                   "public sealed record CreateDemoRequest(string Name);")
        result = self.run_guard()
        self.assertEqual(0, result.returncode, result.stdout)

    def test_original_combined_contracts_file_is_rejected(self):
        self.write("src/Modules/Demo/Contracts/TenantRequests.cs",
                   "public record CreateDemoRequest(string Name);\n"
                   "public record RenameDemoRequest(string Name);")
        result = self.run_guard()
        self.assertNotEqual(0, result.returncode)
        self.assertIn("Presentation/Requests, never Contracts", result.stdout)

    def test_renaming_the_contracts_file_does_not_bypass_the_guard(self):
        self.write("src/Modules/Demo/Contracts/Payload.cs",
                   "public record CreateDemoRequest(string Name);")
        result = self.run_guard()
        self.assertNotEqual(0, result.returncode)
        self.assertIn("Presentation/Requests, never Contracts", result.stdout)

    def test_combined_http_types_in_presentation_are_rejected(self):
        self.write("src/Modules/Demo/Presentation/Requests/DemoRequests.cs",
                   "public record CreateDemoRequest(string Name);\n"
                   "public record RenameDemoRequest(string Name);")
        result = self.run_guard()
        self.assertNotEqual(0, result.returncode)
        self.assertIn("each HTTP request DTO needs its own matching file", result.stdout)


if __name__ == "__main__":
    unittest.main()
