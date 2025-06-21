# browser_action Tool Specification

## Purpose
Interact with websites through a Puppeteer-controlled browser. This tool enables web development testing, verification, and interaction with web applications and HTML files.

## Parameters
- `action` (required): The action to perform (launch, click, type, scroll_down, scroll_up, close)
- `url` (optional): URL for the launch action
- `coordinate` (optional): X,Y coordinates for click actions
- `text` (optional): Text string for type actions

## Usage
```xml
<browser_action>
<action>launch</action>
<url>http://localhost:3000</url>
</browser_action>
```

## Available Actions

### launch
Launch a new browser instance at specified URL. **Must always be the first action.**
```xml
<browser_action>
<action>launch</action>
<url>http://localhost:3000/page</url>
</browser_action>
```

### click
Click at specific X,Y coordinates (within 900x600 resolution).
```xml
<browser_action>
<action>click</action>
<coordinate>450,300</coordinate>
</browser_action>
```

### type
Type text on the keyboard (typically after clicking a text field).
```xml
<browser_action>
<action>type</action>
<text>Hello, world!</text>
</browser_action>
```

### scroll_down
Scroll down the page by one page height.
```xml
<browser_action>
<action>scroll_down</action>
</browser_action>
```

### scroll_up
Scroll up the page by one page height.
```xml
<browser_action>
<action>scroll_up</action>
</browser_action>
```

### close
Close the browser instance. **Must always be the final action.**
```xml
<browser_action>
<action>close</action>
</browser_action>
```

## Browser Specifications
- **Resolution**: 900x600 pixels
- **Response**: Each action returns a screenshot and console logs
- **Sequence**: Must start with launch and end with close
- **Exclusivity**: Only browser_action can be used while browser is active

## Best Practices
- Always start with launch and end with close
- Click at center of elements, not edges
- Use screenshots to determine click coordinates
- Wait for user response between actions
- Use for verification after implementing features

## Common Use Cases
- Testing web applications after development
- Verifying component rendering and functionality
- Interacting with forms and user interfaces
- Checking responsive design and layout
- Debugging web application issues
- Demonstrating completed features

## Workflow Pattern
1. **Launch** browser at target URL
2. **Interact** with page elements (click, type, scroll)
3. **Verify** functionality through screenshots
4. **Close** browser when testing complete

## Error Handling
- Returns error if invalid coordinates are used
- May fail if page elements are not loaded
- Handles network timeouts and connection issues
- Provides console logs for debugging

## URL Types Supported
- Local development servers (http://localhost:3000)
- Local HTML files (file:///path/to/file.html)
- Remote websites (https://example.com)
- Any valid HTTP/HTTPS URL

## Integration with Other Tools
- Often used after `execute_command` to test running applications
- Frequently follows file creation/modification to verify results
- Used with development servers started via execute_command
- Complements code changes for end-to-end verification

## Example Workflow

### Testing a Web Application
```xml
<!-- Start browser -->
<browser_action>
<action>launch</action>
<url>http://localhost:3000</url>
</browser_action>

<!-- Click a button -->
<browser_action>
<action>click</action>
<coordinate>200,150</coordinate>
</browser_action>

<!-- Type in a form field -->
<browser_action>
<action>type</action>
<text>test input</text>
</browser_action>

<!-- Close browser -->
<browser_action>
<action>close</action>
</browser_action>
```

## Limitations
- Fixed 900x600 resolution
- Cannot open multiple browser instances simultaneously
- Must close before using other tools
- Requires valid URLs and proper page loading
- Click coordinates must be determined from screenshots

## Performance Considerations
- Browser startup may take a few seconds
- Page loading time affects interaction timing
- Screenshots are generated for each action
- Network connectivity affects remote URL access
