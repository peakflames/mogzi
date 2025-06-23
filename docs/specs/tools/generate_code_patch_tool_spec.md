# generate_code_patch Tool Specification

## Purpose
Generate a unified diff patch showing the changes between original and modified content. This tool creates Git-style patches that can be applied later using `apply_code_patch` or reviewed for validation.

## Parameters
- `path` (required): The path of the file to generate a patch for
- `modifiedContent` (required): The modified content that should replace the current file content
- `contextLines` (optional): Number of context lines to include around changes (default: 3)

## Usage
```xml
<generate_code_patch>
<path>src/config.js</path>
<modifiedContent>const API_URL = 'https://api.production.com';
const DEBUG = false;
const VERSION = '1.0.0';

module.exports = {
  API_URL,
  DEBUG,
  VERSION
};
</modifiedContent>
<contextLines>3</contextLines>
</generate_code_patch>
```

## Capabilities
- Generates standard unified diff patches
- Configurable context lines for better readability
- Handles additions, deletions, and modifications
- Creates patches compatible with Git and other version control systems
- Provides detailed change statistics

## Output Format
The tool returns a unified diff patch in standard format:
```
--- a/src/config.js
+++ b/src/config.js
@@ -1,3 +1,4 @@
-const API_URL = 'http://localhost:3000';
+const API_URL = 'https://api.production.com';
 const DEBUG = false;
+const VERSION = '1.0.0';
 
```

## Context Lines
Context lines provide surrounding code for better understanding:
- **Default**: 3 lines before and after changes
- **Minimum**: 0 lines (shows only changed lines)
- **Maximum**: Reasonable limit to avoid excessive output
- **Purpose**: Helps locate changes and understand impact

## Best Practices
- Use appropriate context lines for readability (3 is usually optimal)
- Generate patches for review before applying changes
- Combine with `preview_patch_application` to validate patches
- Store patches for version control or documentation purposes

## Common Use Cases
- Creating patches for code review
- Generating changes for version control systems
- Documenting modifications for audit trails
- Preparing changes for batch application
- Creating reusable modification templates

## Change Types Handled

### Additions
```xml
<generate_code_patch>
<path>src/utils.js</path>
<modifiedContent>function existingFunction() {
  return 'existing';
}

function newFunction() {
  return 'new';
}
</modifiedContent>
</generate_code_patch>
```

### Deletions
```xml
<generate_code_patch>
<path>src/deprecated.js</path>
<modifiedContent>function keepThis() {
  return 'keep';
}
</modifiedContent>
</generate_code_patch>
```

### Modifications
```xml
<generate_code_patch>
<path>src/config.js</path>
<modifiedContent>const API_URL = 'https://api.production.com';
const DEBUG = true;
</modifiedContent>
</generate_code_patch>
```

## Error Handling
- Returns error if file doesn't exist
- Returns error if path is outside working directory
- Handles empty files gracefully
- Provides clear error messages for invalid inputs

## Output Statistics
The tool provides detailed statistics:
- **Hunks**: Number of change blocks in the patch
- **Lines Added**: Total lines added
- **Lines Removed**: Total lines removed
- **Context Lines**: Number of context lines included

## Integration with Other Tools
- Output can be used directly with `apply_code_patch`
- Often preceded by `read_file` to understand current content
- Can be combined with `preview_patch_application` for validation
- Useful with `search_files` to identify files needing changes

## Advanced Examples

### Multiple Changes
```xml
<generate_code_patch>
<path>package.json</path>
<modifiedContent>{
  "name": "my-project",
  "version": "2.0.0",
  "description": "Updated project description",
  "scripts": {
    "start": "node server.js",
    "test": "jest",
    "build": "webpack --mode production",
    "dev": "nodemon server.js"
  },
  "dependencies": {
    "express": "^4.18.0",
    "lodash": "^4.17.21"
  }
}
</modifiedContent>
<contextLines>2</contextLines>
</generate_code_patch>
```

### Code Refactoring
```xml
<generate_code_patch>
<path>src/api.js</path>
<modifiedContent>import axios from 'axios';

class ApiClient {
  constructor(baseURL) {
    this.client = axios.create({ baseURL });
  }

  async get(endpoint) {
    const response = await this.client.get(endpoint);
    return response.data;
  }

  async post(endpoint, data) {
    const response = await this.client.post(endpoint, data);
    return response.data;
  }
}

export default ApiClient;
</modifiedContent>
</generate_code_patch>
```

### Configuration Updates
```xml
<generate_code_patch>
<path>.env.example</path>
<modifiedContent>NODE_ENV=production
API_URL=https://api.production.com
DATABASE_URL=postgresql://user:pass@localhost/db
JWT_SECRET=your-secret-key
REDIS_URL=redis://localhost:6379
LOG_LEVEL=info
</modifiedContent>
<contextLines>1</contextLines>
</generate_code_patch>
```

## Patch Validation
Generated patches can be validated by:
1. Using `preview_patch_application` to test application
2. Reviewing the unified diff format for correctness
3. Checking change statistics for expected modifications
4. Verifying context lines provide adequate surrounding code

## Security Considerations
- Working directory constraints are enforced
- Path traversal attacks are prevented
- File permissions are respected
- No sensitive information is exposed in patches

## Performance Considerations
- Efficient diff algorithms for large files
- Configurable context lines to control output size
- Memory-efficient processing for substantial changes
- Reasonable limits on file sizes and change complexity

## Verification Methods
- **Test**: Automated tests verify patch generation accuracy
- **Analysis**: Static analysis of generated patch format
- **Demonstration**: Live demonstration of patch creation
- **Inspection**: Manual review of generated patches

## Related Tools
- `apply_code_patch` - Apply patches generated by this tool
- `preview_patch_application` - Preview patch application
- `read_file` - Examine current file content
- `write_to_file` - Alternative for complete file replacement

## Workflow Integration
Typical workflow using this tool:
1. `read_file` - Examine current content
2. `generate_code_patch` - Create patch with desired changes
3. `preview_patch_application` - Validate patch (optional)
4. `apply_code_patch` - Apply the generated patch
