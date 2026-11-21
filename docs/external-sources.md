# External Sources Documentation

External sources allow Flamenco to incorporate code from external repositories into your package builds. This is useful when you need to include third-party dependencies, submodules, or other external code that isn't part of your main source tree.

## Overview

The external sources system downloads and prepares external code before building your package. Each external source is defined in a JSON descriptor file that specifies where to obtain the code and how to prepare it.

## Supported Source Types

### Git External Source

The Git external source clones a Git repository and optionally checks out a specific commit, tag, or branch.

#### JSON Example

```json
{
    "type": "external_source",
    "sourceType": "git",
    "repository": "https://github.com/canonical/dotnet-test-runner",
    "commitish": "v1.2.0",
    "rootDirectory": "src/main",
    "postClone": [
        "make GIT_COMMIT_ID",
        "make GIT_TAG_VERSION"
    ],
    "ignoredFiles": [
        ".gitignore",
        ".github/workflows/build.yml"
    ]
}
```

#### Field Reference

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | string | No | Descriptor file type identifier. Can be `"external_source"` or omitted. |
| `sourceType` | string | **Yes** | The type of external source. Must be `"git"` for Git repositories. |
| `repository` | string | **Yes** | The URL of the Git repository to clone. Supports HTTPS and SSH URLs. |
| `commitish` | string | No | A commit SHA, tag, or branch name to check out after cloning. If omitted, the default branch is used. |
| `rootDirectory` | string | No | A relative path to a subdirectory within the repository to use as the source root. Only this subdirectory will be copied to the destination. |
| `postClone` | array of strings | No | Shell commands to execute after cloning and checking out. Runs in the cloned repository directory. |
| `ignoredFiles` | array of strings | No | Relative paths of files or directories to delete from the cloned repository before copying to the build directory. |

#### Field Details

##### `sourceType` (Required)

Specifies the type of external source. Use `"git"` for Git external sources.

**Example:**
```json
"sourceType": "git"
```

##### `repository` (Required)

The URL of the Git repository to clone. This can be any URL format supported by Git, including HTTPS and SSH.

**Examples:**
```json
"repository": "https://github.com/username/repo.git"
"repository": "https://gitlab.com/group/project.git"
"repository": "git@github.com:username/repo.git"
```

##### `commitish` (Optional)

A Git reference to check out after cloning. This can be:
- A commit SHA (full or abbreviated): `"7a9f3e2b"`
- A tag: `"v1.2.0"`, `"release-2023-12"`
- A branch name: `"main"`, `"develop"`, `"feature/new-api"`

If omitted, the repository's default branch (typically `main` or `master`) is used.

**Examples:**
```json
"commitish": "v1.2.0"
"commitish": "7a9f3e2b4c1d"
"commitish": "main"
```

##### `rootDirectory` (Optional)

A relative path to a subdirectory within the cloned repository. When specified, only the contents of this subdirectory will be copied to the destination build directory, rather than the entire repository.

This is useful when:
- The repository contains multiple projects or packages, and you only need one
- The actual source code is nested within subdirectories
- You want to exclude top-level configuration or documentation files

The path is relative to the repository root and should use forward slashes (`/`) as path separators, even on Windows.

**Important notes:**
- The directory must exist in the cloned repository, or an error (FL0052) will be raised
- The `ignoredFiles` paths are relative to the repository root, not the `rootDirectory`
- Post-clone commands run in the repository root, before the `rootDirectory` is applied

**Examples:**
```json
"rootDirectory": "src"
"rootDirectory": "packages/core"
"rootDirectory": "modules/main/source"
```

**Example use case:**
```json
{
    "repository": "https://github.com/example/monorepo",
    "rootDirectory": "packages/cli",
    "ignoredFiles": [
        "packages/web/",
        "packages/mobile/"
    ]
}
```

##### `postClone` (Optional)

An array of shell commands to execute after the repository has been cloned and checked out. Commands are executed sequentially using `/usr/bin/env sh -c`, which provides a portable shell environment.

Commands run in the context of the cloned repository directory. If any command exits with a non-zero status code, the process fails immediately and subsequent commands are not executed.

**Common use cases:**
- Generate version information: `"make GIT_COMMIT_ID"`
- Build generated files: `"./configure && make"`
- Download dependencies: `"npm install --production"`
- Prepare source files: `"python setup.py build"`

**Example:**
```json
"postClone": [
    "chmod +x build.sh",
    "./build.sh --prepare",
    "make VERSION_FILE"
]
```

##### `ignoredFiles` (Optional)

An array of relative file or directory paths to delete from the cloned repository before it's copied to the build directory. This is useful for removing files or directories that shouldn't be included in the package, such as CI configuration, documentation, or test files.

Paths are relative to the repository root. Both files and directories are supported:
- **Files**: Individual files are deleted
- **Directories**: Entire directories and their contents are deleted recursively

If a specified path doesn't exist (neither as a file nor directory), it is silently ignored (similar to how `.gitignore` works).

**Common use cases:**
- Remove CI configuration: `".github/workflows/build.yml"`, `".github/"`
- Remove Git metadata: `".gitignore"`, `".gitattributes"`
- Remove documentation: `"docs/"`, `"README.md"`
- Remove test files: `"tests/"`, `"*.test.js"`

**Example:**
```json
"ignoredFiles": [
    ".gitignore",
    ".github/workflows/build.yml",
    "tests/",
    "docs/README.md"
]
```

## Error Reference

The following errors can occur during external source processing:

### General External Sources errors

#### FL0043: Unsupported external source type

**Cause**: The `sourceType` field contains a value other than `"git"`.

**Example:**
```json
"sourceType": "svn"  // Not supported
```

**Resolution**: Use `"git"` as the source type, or wait for additional source types to be implemented.

---

#### FL0044: Unspecified external source type

**Cause**: The `sourceType` field is missing from the descriptor file.

**Example:**
```json
{
    "type": "external_source",
    "repository": "https://github.com/user/repo"
    // Missing "sourceType" field
}
```

**Resolution**: Add the `sourceType` field with the value `"git"`.

---

#### FL0045: Invalid git external source descriptor

**Cause**: Required fields are missing or invalid in the Git external source descriptor.

**Common causes:**
- Missing `repository` field
- Invalid JSON structure
- Incorrect field types (e.g., string instead of array)

**Example of invalid descriptor:**
```json
{
    "sourceType": "git"
    // Missing required "repository" field
}
```

**Resolution**: Ensure all required fields are present and have the correct types. The error metadata will indicate which fields are invalid.

---

#### Operation Canceled

**Cause**: The operation was canceled via the `CancellationToken`.

**Common causes:**
- User-initiated cancellation
- Timeout
- Application shutdown

**Resolution**: This is typically an expected behavior when operations are intentionally canceled. No action is usually required.

---

### Git External Sources errors

#### FL0050: Git clone failed

**Cause**: The Git clone or checkout operation failed.

**Common causes:**
- Invalid repository URL
- Network connectivity issues
- Authentication failure (for private repositories)
- Invalid `commitish` (commit, tag, or branch doesn't exist)
- Insufficient disk space
- Permission issues with the cache directory

**Resolution**: 
- Verify the repository URL is correct and accessible
- Check network connectivity
- For private repositories, ensure proper authentication is configured
- Verify the `commitish` exists in the repository
- Ensure sufficient disk space and write permissions

---

#### FL0051: Repository copy from cache failed

**Cause**: Failed to copy the cached repository to the destination directory.

**Common causes:**
- Insufficient disk space
- Permission issues with the destination directory
- Destination directory is locked by another process
- Filesystem errors

**Resolution**:
- Ensure sufficient disk space is available
- Verify write permissions on the destination directory
- Check for filesystem errors
- Ensure no other process is locking the destination

---

#### FL0052: Root directory not found

**Cause**: The directory specified in `rootDirectory` does not exist in the cloned repository.

**Example:**
```json
{
    "repository": "https://github.com/user/repo",
    "rootDirectory": "src/non-existent"  // Directory doesn't exist
}
```

**Resolution**:
- Verify the path is correct and exists in the repository
- Check for typos in the directory name (paths are case-sensitive on Linux/macOS)
- Ensure the directory exists in the specific commit/tag/branch you're checking out
- Clone the repository manually and verify the directory structure

---

#### FL0053: Post-clone command failed

**Cause**: A command specified in `postClone` exited with a non-zero status code.

**Example:**
```json
"postClone": ["make build"]  // Exits with code 2
```

**Resolution**:
- Check the command syntax and ensure it's valid
- Verify all dependencies required by the command are available
- Test the command manually in a cloned repository
- Review command output for specific error messages
- Ensure the command is appropriate for the shell environment (`/usr/bin/env sh -c`)

---

#### FL0054: Deletion of ignored file failed

**Cause**: Failed to delete a file or directory specified in `ignoredFiles`.

**Common causes:**
- File or directory is read-only or immutable
- Permission issues
- File is locked by another process
- Directory contains locked files
- Filesystem errors

**Resolution**:
- Check file/directory permissions
- Ensure no other process is using the file or files within the directory
- Verify filesystem integrity
- Consider removing the path from `ignoredFiles` if it's not critical

## Best Practices

### 1. Pin to Specific Versions

Always use a specific commit SHA or tag in the `commitish` field for reproducible builds:

```json
"commitish": "v1.2.0"  // Good: specific tag
"commitish": "main"    // Avoid: branch tip can change
```

### 2. Minimize Post-Clone Commands

Keep post-clone commands simple and fast. Complex build processes should be deferred to the main build phase:

```json
// Good: Simple preparation tasks
"postClone": [
    "make generate-version",
    "chmod +x build.sh"
]

// Avoid: Long-running builds
"postClone": [
    "npm install && npm run build && npm test"  // Too much work
]
```

### 3. Clean Up Unnecessary Files

Use `ignoredFiles` to remove files that bloat the source package:

```json
"ignoredFiles": [
    ".git",
    ".github/",
    "tests/",
    "docs/",
    ".gitignore",
    ".gitattributes",
    "*.test.js"
]
```

### 4. Use HTTPS for Public Repositories

For public repositories, prefer HTTPS URLs for better compatibility:

```json
"repository": "https://github.com/user/repo.git"  // Preferred
"repository": "git@github.com:user/repo.git"     // Requires SSH keys
```

### 5. Test Commands Independently

Before adding commands to `postClone`, test them manually in a cloned repository to ensure they work as expected.

## Example: Complete Workflow

Here's a complete example demonstrating all features:

```json
{
    "type": "external_source",
    "sourceType": "git",
    "repository": "https://github.com/canonical/dotnet-test-runner",
    "commitish": "v1.2.0",
    "rootDirectory": "src",
    "postClone": [
        "make GIT_COMMIT_ID",
        "make GIT_TAG_VERSION",
        "chmod +x scripts/*.sh"
    ],
    "ignoredFiles": [
        ".gitignore",
        ".gitattributes",
        ".github/",
        "tests/",
        "docs/",
        "*.md",
        ".editorconfig"
    ]
}
```

**This configuration will:**
1. Clone the `dotnet-test-runner` repository
2. Check out the `v1.2.0` tag
3. Execute `make GIT_COMMIT_ID` to generate version information
4. Execute `make GIT_TAG_VERSION` to embed the tag version
5. Make all shell scripts in `scripts/` executable
6. Delete Git metadata, CI config, tests, documentation, and editor config from the repository root
7. Remove the `.git` directory
8. Use only the `src` subdirectory as the source (instead of the entire repository)
9. Cache the processed result for future use
10. Copy the final result to the destination directory

## Future Enhancements

The external sources system is designed to be extensible. Potential future source types include:

- **HTTP/HTTPS**: Download and extract tarballs or zip files
- **Bazaar**: Support for Bazaar repositories
- **Subversion**: Support for SVN repositories
- **Mercurial**: Support for Mercurial repositories
- **Local**: Copy from a local directory

