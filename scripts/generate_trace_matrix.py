#!/usr/bin/env python3
"""
Requirements Trace Matrix Generator

This script automatically generates a requirements trace matrix by:
1. Parsing requirements from docs/process/02_operational_requirements.md
2. Scanning test files for requirement ID comments (e.g., // TOR-1.1)
3. Generating a markdown report at outputs/latest_rqmts_trace_matrix.md

Usage:
    python scripts/generate_trace_matrix.py

The script can be run from any working directory and has no external dependencies.
"""

import os
import re
import sys
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Set, Tuple, Optional

# Check for markdown library
try:
    import markdown
    MARKDOWN_AVAILABLE = True
except ImportError:
    MARKDOWN_AVAILABLE = False


def find_project_root() -> Path:
    """Find the project root directory based on the script's location."""
    # The script is in scripts/ directory, so project root is one level up
    script_dir = Path(__file__).parent  # scripts/
    project_root = script_dir.parent    # project root
    
    # Verify this is actually the project root by checking for key files
    indicators = ['src/Mogzi.sln', 'docs/process/02_operational_requirements.md']
    if all((project_root / indicator).exists() for indicator in indicators):
        return project_root
    
    # Fallback: if the expected structure doesn't exist, search upward from script location
    for path in [script_dir] + list(script_dir.parents):
        if all((path / indicator).exists() for indicator in indicators):
            return path
    
    # Final fallback: use current working directory
    return Path.cwd()


def parse_requirements(requirements_file: Path) -> Dict[str, str]:
    """Parse requirements from the operational requirements file."""
    requirements = {}
    
    if not requirements_file.exists():
        print(f"Warning: Requirements file not found at {requirements_file}")
        return requirements
    
    try:
        with open(requirements_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Split content into sections and parse each requirement block
        lines = content.split('\n')
        current_req_id = None
        current_req_text = ""
        current_priority = "Unknown"
        current_impl_status = "Unknown"
        
        for line in lines:
            # Check for requirement ID line
            req_match = re.match(r'-\s*\*\*([A-Z]+-[\d.]+)\*\*:\s*(.+)', line)
            if req_match:
                # Save previous requirement if exists
                if current_req_id:
                    full_description = f"{current_req_text} - **Priority**: {current_priority} - **Impl Status**: {current_impl_status}"
                    requirements[current_req_id] = full_description.strip()
                
                # Start new requirement
                current_req_id = req_match.group(1)
                current_req_text = req_match.group(2).strip()
                current_priority = "Unknown"
                current_impl_status = "Unknown"
            
            # Check for priority line
            elif current_req_id and re.match(r'\s*-\s*\*\*Priority\*\*:\s*(.+)', line):
                priority_match = re.match(r'\s*-\s*\*\*Priority\*\*:\s*(.+)', line)
                if priority_match:
                    current_priority = priority_match.group(1).strip()
            
            # Check for implementation status line
            elif current_req_id and re.match(r'\s*-\s*\*\*Impl Status\*\*:\s*(.+)', line):
                impl_match = re.match(r'\s*-\s*\*\*Impl Status\*\*:\s*(.+)', line)
                if impl_match:
                    current_impl_status = impl_match.group(1).strip()
        
        # Don't forget the last requirement
        if current_req_id:
            full_description = f"{current_req_text} - **Priority**: {current_priority} - **Impl Status**: {current_impl_status}"
            requirements[current_req_id] = full_description.strip()
        
        print(f"Parsed {len(requirements)} requirements from {requirements_file}")
        
    except Exception as e:
        print(f"Error parsing requirements file: {e}")
    
    return requirements


def scan_test_files(test_directories: List[Path]) -> Dict[str, List[Tuple[str, str]]]:
    """
    Scan test files in multiple directories for requirement ID comments.
    Returns a dict mapping requirement IDs to list of (file_path, test_method) tuples.
    """
    requirement_tests = {}
    
    for test_directory in test_directories:
        if not test_directory.exists():
            print(f"Warning: Test directory not found at {test_directory}")
            continue
        
        # Pattern to match requirement ID comments like // TOR-1.1 or // TOR-1.1, TOR-2.3
        req_comment_pattern = r'//\s*([A-Z]+-[\d.]+(?:\s*,\s*[A-Z]+-[\d.]+)*)'
        
        # Pattern to find test method names
        test_method_pattern = r'public\s+(?:async\s+)?Task\s+(\w+)\s*\('
        
        for test_file in test_directory.glob('**/*.cs'):
            try:
                with open(test_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                lines = content.split('\n')
                current_test_method = None
                
                for i, line in enumerate(lines):
                    # Check for test method definition
                    method_match = re.search(test_method_pattern, line)
                    if method_match:
                        current_test_method = method_match.group(1)
                    
                    # Check for requirement ID comments
                    req_match = re.search(req_comment_pattern, line)
                    if req_match and current_test_method:
                        # Parse multiple requirement IDs from the comment
                        req_ids_text = req_match.group(1)
                        req_ids = [req_id.strip() for req_id in req_ids_text.split(',')]
                        
                        relative_path = test_file.relative_to(test_directory.parent.parent)
                        
                        for req_id in req_ids:
                            if req_id not in requirement_tests:
                                requirement_tests[req_id] = []
                            
                            # Avoid duplicates
                            test_info = (str(relative_path), current_test_method)
                            if test_info not in requirement_tests[req_id]:
                                requirement_tests[req_id].append(test_info)
            
            except Exception as e:
                print(f"Error scanning test file {test_file}: {e}")
    
    total_mappings = sum(len(tests) for tests in requirement_tests.values())
    print(f"Found {total_mappings} requirement-to-test mappings across {len(requirement_tests)} requirements")
    
    return requirement_tests


def generate_html_trace_matrix(requirements: Dict[str, str], 
                              requirement_tests: Dict[str, List[Tuple[str, str]]],
                              html_output_file: Path) -> None:
    """Generate the requirements trace matrix HTML file with dark theme."""
    
    # Ensure output directory exists
    html_output_file.parent.mkdir(parents=True, exist_ok=True)
    
    # Calculate coverage statistics
    total_requirements = len(requirements)
    covered_requirements = len([req_id for req_id in requirements.keys() if req_id in requirement_tests])
    coverage_percentage = (covered_requirements / total_requirements * 100) if total_requirements > 0 else 0
    
    # Calculate coverage by priority
    priority_stats = {}
    for req_id, description in requirements.items():
        # Extract priority
        priority_match = re.search(r'\*\*Priority\*\*:\s*(\w+)', description)
        priority = priority_match.group(1) if priority_match else "Unknown"
        
        if priority not in priority_stats:
            priority_stats[priority] = {'total': 0, 'covered': 0}
        
        priority_stats[priority]['total'] += 1
        if req_id in requirement_tests:
            priority_stats[priority]['covered'] += 1
    
    # Calculate stats for implemented requirements
    implemented_reqs = {req_id: desc for req_id, desc in requirements.items() if "Impl Status**: Implemented" in desc}
    total_implemented = len(implemented_reqs)
    covered_implemented = len([req_id for req_id in implemented_reqs.keys() if req_id in requirement_tests])
    coverage_implemented_percentage = (covered_implemented / total_implemented * 100) if total_implemented > 0 else 0

    # Calculate coverage by priority for all requirements
    priority_stats_all = {}
    for req_id, description in requirements.items():
        priority_match = re.search(r'\*\*Priority\*\*:\s*(\w+)', description)
        priority = priority_match.group(1) if priority_match else "Unknown"
        if priority not in priority_stats_all:
            priority_stats_all[priority] = {'total': 0, 'covered': 0}
        priority_stats_all[priority]['total'] += 1
        if req_id in requirement_tests:
            priority_stats_all[priority]['covered'] += 1

    # Calculate coverage by priority for implemented requirements
    priority_stats_impl = {}
    for req_id, description in implemented_reqs.items():
        priority_match = re.search(r'\*\*Priority\*\*:\s*(\w+)', description)
        priority = priority_match.group(1) if priority_match else "Unknown"
        if priority not in priority_stats_impl:
            priority_stats_impl[priority] = {'total': 0, 'covered': 0}
        priority_stats_impl[priority]['total'] += 1
        if req_id in requirement_tests:
            priority_stats_impl[priority]['covered'] += 1

    # Generate priority coverage summary for both
    priority_summary_all = ""
    priority_summary_impl = ""
    for priority in ['Critical', 'High', 'Medium', 'Low', 'Unknown']:
        if priority in priority_stats_all:
            stats = priority_stats_all[priority]
            coverage_pct = (stats['covered'] / stats['total'] * 100) if stats['total'] > 0 else 0
            priority_summary_all += f"- **{priority}:** {stats['covered']}/{stats['total']} ({coverage_pct:.1f}%)\n"
        if priority in priority_stats_impl:
            stats = priority_stats_impl[priority]
            coverage_pct = (stats['covered'] / stats['total'] * 100) if stats['total'] > 0 else 0
            priority_summary_impl += f"- **{priority}:** {stats['covered']}/{stats['total']} ({coverage_pct:.1f}%)\n"

    # Generate the markdown content for conversion
    markdown_content = f"""# Requirements Trace Matrix

**Generated:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S UTC')}

## Summary

| Overall Project Metrics | Implemented Requirements Metrics |
|-------------------------|----------------------------------|
| **Total Rqmts:** {total_requirements} | **Total Rqmts:** {total_implemented} |
| **Covered Rqmts:** {covered_requirements} | **Covered Rqmts:** {covered_implemented} |
| **Coverage:** {coverage_percentage:.1f}% | **Coverage:** {coverage_implemented_percentage:.1f}% |

### Coverage by Priority

| Overall Project | Implemented Rqmts |
|-----------------|-------------------|
| {priority_summary_all.replace(os.linesep, '<br>')} | {priority_summary_impl.replace(os.linesep, '<br>')} |

## Requirements Trace Matrix Table

| Requirement ID | Priority | Impl Status | Requirement Text | Test File | Test Case Function |
|----------------|----------|-------------|------------------|-----------|-------------------|
"""
    
    # Sort requirements by ID for consistent output
    sorted_requirements = sorted(requirements.items())
    
    for req_id, description in sorted_requirements:
        # Parse the description to extract priority and implementation status
        priority = "Unknown"
        impl_status = "Unknown"
        req_text = description
        
        # Extract priority
        priority_match = re.search(r'\*\*Priority\*\*:\s*(\w+)', description)
        if priority_match:
            priority = priority_match.group(1)
        
        # Extract implementation status
        impl_match = re.search(r'\*\*Impl Status\*\*:\s*([^-]+)', description)
        if impl_match:
            impl_status = impl_match.group(1).strip()
        
        # Clean up requirement text (remove metadata)
        req_text = re.sub(r'\s*-\s*\*\*Priority\*\*:[^-]*', '', req_text)
        req_text = re.sub(r'\s*-\s*\*\*Impl Status\*\*:[^-]*', '', req_text)
        req_text = re.sub(r'\s*-\s*\*\*Verification\*\*:[^-]*', '', req_text)
        req_text = req_text.strip()
        
        # Handle test coverage
        if req_id in requirement_tests:
            # Create a row for each test case
            for file_path, test_method in requirement_tests[req_id]:
                # Escape pipe characters in text for markdown table
                safe_req_text = req_text.replace('|', '\\|')
                safe_file_path = file_path.replace('|', '\\|')
                safe_test_method = test_method.replace('|', '\\|')
                
                markdown_content += f"| {req_id} | {priority} | {impl_status} | {safe_req_text} | {safe_file_path} | {safe_test_method} |\n"
        else:
            # No test coverage
            safe_req_text = req_text.replace('|', '\\|')
            markdown_content += f"| {req_id} | {priority} | {impl_status} | {safe_req_text} | ❌ No test coverage | ❌ No test coverage |\n"
    
    markdown_content += f"""
## Generation Details

- **Requirements Source:** `docs/process/02_operational_requirements.md`
- **Test Directory:** `test/`
- **Script:** `scripts/generate_trace_matrix.py`
- **Output:** `{html_output_file.relative_to(find_project_root())}`

This trace matrix is automatically generated by scanning requirement ID comments in test files.
To update coverage, add comments like `// TOR-1.1` to test assertions that validate specific requirements.
"""
    
    # Convert markdown to HTML
    md = markdown.Markdown(extensions=['tables'])
    html_body = md.convert(markdown_content)
    
    # Create full HTML with dark theme
    html_content = f"""<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Requirements Trace Matrix</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #1a1a1a;
            color: #e0e0e0;
            margin: 0;
            padding: 20px;
            line-height: 1.6;
        }}
        
        .container {{
            max-width: 1400px;
            margin: 0 auto;
            background-color: #2d2d2d;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.3);
        }}
        
        h1 {{
            color: #4CAF50;
            border-bottom: 3px solid #4CAF50;
            padding-bottom: 10px;
            margin-bottom: 30px;
        }}
        
        h2 {{
            color: #81C784;
            border-bottom: 2px solid #81C784;
            padding-bottom: 8px;
            margin-top: 40px;
            margin-bottom: 20px;
        }}
        
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
            background-color: #3d3d3d;
            border-radius: 6px;
            overflow: hidden;
        }}
        
        th {{
            background-color: #4CAF50;
            color: white;
            padding: 12px 8px;
            text-align: left;
            font-weight: 600;
            border-bottom: 2px solid #45a049;
        }}
        
        td {{
            padding: 10px 8px;
            border-bottom: 1px solid #555;
            vertical-align: top;
        }}
        
        /* Text wrapping for Test File and Test Case Function columns */
        td:nth-child(5), td:nth-child(6) {{
            word-wrap: break-word;
            word-break: break-word;
            max-width: 250px;
            min-width: 150px;
            white-space: normal;
            overflow-wrap: break-word;
            hyphens: auto;
        }}
        
        /* Better readability for file paths - break at path separators */
        td:nth-child(5) {{
            font-family: 'Courier New', monospace;
            font-size: 0.9em;
        }}
        
        /* Better readability for function names */
        td:nth-child(6) {{
            font-family: 'Courier New', monospace;
            font-size: 0.9em;
        }}
        
        tr:nth-child(even) {{
            background-color: #404040;
        }}
        
        tr:hover {{
            background-color: #4a4a4a;
        }}
        
        .priority-critical {{
            background-color: #d32f2f;
            color: white;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 0.85em;
            font-weight: bold;
        }}
        
        .priority-high {{
            background-color: #f57c00;
            color: white;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 0.85em;
            font-weight: bold;
        }}
        
        .priority-medium {{
            background-color: #1976d2;
            color: white;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 0.85em;
            font-weight: bold;
        }}
        
        .priority-low {{
            background-color: #388e3c;
            color: white;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 0.85em;
            font-weight: bold;
        }}
        
        .status-implemented {{
            background-color: #4CAF50;
            color: white;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 0.85em;
        }}
        
        .status-partial {{
            background-color: #FF9800;
            color: white;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 0.85em;
        }}
        
        .status-not-implemented {{
            background-color: #f44336;
            color: white;
            padding: 2px 6px;
            border-radius: 3px;
            font-size: 0.85em;
        }}
        
        .no-coverage {{
            color: #f44336;
            font-weight: bold;
        }}
        
        code {{
            background-color: #555;
            color: #e0e0e0;
            padding: 2px 4px;
            border-radius: 3px;
            font-family: 'Courier New', monospace;
        }}
        
        ul {{
            margin: 10px 0;
            padding-left: 20px;
        }}
        
        li {{
            margin: 5px 0;
        }}
        
        strong {{
            color: #81C784;
        }}
        
        .summary-stats {{
            background-color: #404040;
            padding: 15px;
            border-radius: 6px;
            margin: 20px 0;
        }}
        
        .generation-details {{
            background-color: #404040;
            padding: 15px;
            border-radius: 6px;
            margin: 20px 0;
            font-size: 0.9em;
        }}
    </style>
</head>
<body>
    <div class="container">
        {html_body}
    </div>
    
    <script>
        // Add CSS classes based on content
        document.addEventListener('DOMContentLoaded', function() {{
            // Style status cells (but not priority cells)
            const cells = document.querySelectorAll('td');
            cells.forEach(cell => {{
                const text = cell.textContent.trim().toLowerCase();
                if (text === 'implemented') {{
                    cell.innerHTML = '<span class="status-implemented">Implemented</span>';
                }} else if (text === 'partial') {{
                    cell.innerHTML = '<span class="status-partial">Partial</span>';
                }} else if (text === 'not implemented') {{
                    cell.innerHTML = '<span class="status-not-implemented">Not Implemented</span>';
                }} else if (text.includes('❌ no test coverage')) {{
                    cell.classList.add('no-coverage');
                }}
            }});
            
            // Style summary section
            const summarySection = document.querySelector('h2');
            if (summarySection && summarySection.textContent.includes('Summary')) {{
                const nextElement = summarySection.nextElementSibling;
                if (nextElement && nextElement.tagName === 'UL') {{
                    nextElement.classList.add('summary-stats');
                }}
            }}
            
            // Style generation details
            const detailsSection = Array.from(document.querySelectorAll('h2')).find(h => h.textContent.includes('Generation Details'));
            if (detailsSection) {{
                const nextElement = detailsSection.nextElementSibling;
                if (nextElement && nextElement.tagName === 'UL') {{
                    nextElement.classList.add('generation-details');
                }}
            }}
        }});
    </script>
</body>
</html>"""
    
    # Write the HTML file
    try:
        with open(html_output_file, 'w', encoding='utf-8') as f:
            f.write(html_content)
        
        print(f"🌐 HTML trace matrix generated: {html_output_file}")
        
    except Exception as e:
        print(f"❌ Error writing HTML output file: {e}")
        sys.exit(1)


def generate_trace_matrix(requirements: Dict[str, str], 
                         requirement_tests: Dict[str, List[Tuple[str, str]]],
                         output_file: Path) -> None:
    """Generate the requirements trace matrix markdown file."""
    
    # Ensure output directory exists
    output_file.parent.mkdir(parents=True, exist_ok=True)
    
    # Calculate coverage statistics
    total_requirements = len(requirements)
    covered_requirements = len([req_id for req_id in requirements.keys() if req_id in requirement_tests])
    coverage_percentage = (covered_requirements / total_requirements * 100) if total_requirements > 0 else 0
    
    # Calculate coverage by priority
    priority_stats = {}
    for req_id, description in requirements.items():
        # Extract priority
        priority_match = re.search(r'\*\*Priority\*\*:\s*(\w+)', description)
        priority = priority_match.group(1) if priority_match else "Unknown"
        
        if priority not in priority_stats:
            priority_stats[priority] = {'total': 0, 'covered': 0}
        
        priority_stats[priority]['total'] += 1
        if req_id in requirement_tests:
            priority_stats[priority]['covered'] += 1
    
    # Calculate stats for implemented requirements
    implemented_reqs = {req_id: desc for req_id, desc in requirements.items() if "Impl Status**: Implemented" in desc}
    total_implemented = len(implemented_reqs)
    covered_implemented = len([req_id for req_id in implemented_reqs.keys() if req_id in requirement_tests])
    coverage_implemented_percentage = (covered_implemented / total_implemented * 100) if total_implemented > 0 else 0

    # Calculate coverage by priority for all requirements
    priority_stats_all = {}
    for req_id, description in requirements.items():
        priority_match = re.search(r'\*\*Priority\*\*:\s*(\w+)', description)
        priority = priority_match.group(1) if priority_match else "Unknown"
        if priority not in priority_stats_all:
            priority_stats_all[priority] = {'total': 0, 'covered': 0}
        priority_stats_all[priority]['total'] += 1
        if req_id in requirement_tests:
            priority_stats_all[priority]['covered'] += 1

    # Calculate coverage by priority for implemented requirements
    priority_stats_impl = {}
    for req_id, description in implemented_reqs.items():
        priority_match = re.search(r'\*\*Priority\*\*:\s*(\w+)', description)
        priority = priority_match.group(1) if priority_match else "Unknown"
        if priority not in priority_stats_impl:
            priority_stats_impl[priority] = {'total': 0, 'covered': 0}
        priority_stats_impl[priority]['total'] += 1
        if req_id in requirement_tests:
            priority_stats_impl[priority]['covered'] += 1

    # Generate priority coverage summary for both
    priority_summary_all = ""
    priority_summary_impl = ""
    for priority in ['Critical', 'High', 'Medium', 'Low', 'Unknown']:
        if priority in priority_stats_all:
            stats = priority_stats_all[priority]
            coverage_pct = (stats['covered'] / stats['total'] * 100) if stats['total'] > 0 else 0
            priority_summary_all += f"- **{priority}:** {stats['covered']}/{stats['total']} ({coverage_pct:.1f}%)\n"
        if priority in priority_stats_impl:
            stats = priority_stats_impl[priority]
            coverage_pct = (stats['covered'] / stats['total'] * 100) if stats['total'] > 0 else 0
            priority_summary_impl += f"- **{priority}:** {stats['covered']}/{stats['total']} ({coverage_pct:.1f}%)\n"

    # Generate the markdown content
    content = f"""# Requirements Trace Matrix

**Generated:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S UTC')}

## Summary

| Overall Project Metrics | Implemented Requirements Metrics |
|-------------------------|----------------------------------|
| **Total Rqmts:** {total_requirements} | **Total Rqmts:** {total_implemented} |
| **Covered Rqmts:** {covered_requirements} | **Covered Rqmts:** {covered_implemented} |
| **Coverage:** {coverage_percentage:.1f}% | **Coverage:** {coverage_implemented_percentage:.1f}% |

### Coverage by Priority

| Overall Project | Implemented Rqmts |
|-----------------|-------------------|
| {priority_summary_all.replace(os.linesep, '<br>')} | {priority_summary_impl.replace(os.linesep, '<br>')} |

## Requirements Trace Matrix Table

| Requirement ID | Priority | Implementation Status | Requirement Text | Test File | Test Case Function |
|----------------|----------|----------------------|------------------|-----------|-------------------|
"""
    
    # Sort requirements by ID for consistent output
    sorted_requirements = sorted(requirements.items())
    
    for req_id, description in sorted_requirements:
        # Parse the description to extract priority and implementation status
        priority = "Unknown"
        impl_status = "Unknown"
        req_text = description
        
        # Extract priority
        priority_match = re.search(r'\*\*Priority\*\*:\s*(\w+)', description)
        if priority_match:
            priority = priority_match.group(1)
        
        # Extract implementation status
        impl_match = re.search(r'\*\*Impl Status\*\*:\s*([^-]+)', description)
        if impl_match:
            impl_status = impl_match.group(1).strip()
        
        # Clean up requirement text (remove metadata)
        req_text = re.sub(r'\s*-\s*\*\*Priority\*\*:[^-]*', '', req_text)
        req_text = re.sub(r'\s*-\s*\*\*Impl Status\*\*:[^-]*', '', req_text)
        req_text = re.sub(r'\s*-\s*\*\*Verification\*\*:[^-]*', '', req_text)
        req_text = req_text.strip()
        
        # Handle test coverage
        if req_id in requirement_tests:
            # Create a row for each test case
            for file_path, test_method in requirement_tests[req_id]:
                # Escape pipe characters in text for markdown table
                safe_req_text = req_text.replace('|', '\\|')
                safe_file_path = file_path.replace('|', '\\|')
                safe_test_method = test_method.replace('|', '\\|')
                
                content += f"| {req_id} | {priority} | {impl_status} | {safe_req_text} | {safe_file_path} | {safe_test_method} |\n"
        else:
            # No test coverage
            safe_req_text = req_text.replace('|', '\\|')
            content += f"| {req_id} | {priority} | {impl_status} | {safe_req_text} | ❌ No test coverage | ❌ No test coverage |\n"
    
    content += f"""
## Generation Details

- **Requirements Source:** `docs/process/02_operational_requirements.md`
- **Test Directory:** `test/`
- **Script:** `scripts/generate_trace_matrix.py`
- **Output:** `{output_file.relative_to(find_project_root())}`

This trace matrix is automatically generated by scanning requirement ID comments in test files.
To update coverage, add comments like `// TOR-1.1` to test assertions that validate specific requirements.
"""
    
    # Write the file
    try:
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(content)
        
        print(f"✅ Requirements trace matrix generated: {output_file}")
        print(f"📊 Coverage: {covered_requirements}/{total_requirements} requirements ({coverage_percentage:.1f}%)")
        
    except Exception as e:
        print(f"❌ Error writing output file: {e}")
        sys.exit(1)


def main():
    """Main function to generate the requirements trace matrix."""
    print("🚀 Generating Requirements Trace Matrix...")
    
    # Check for required dependencies early
    if not MARKDOWN_AVAILABLE:
        print("❌ Error: 'markdown' package is required for HTML generation.")
        print("📦 Install it with: pip install markdown")
        sys.exit(1)
    
    # Find project root
    project_root = find_project_root()
    print(f"📁 Project root: {project_root}")
    
    # Define file paths
    requirements_file = project_root / "docs" / "process" / "02_operational_requirements.md"
    test_directories = [
        project_root / "test" / "Mogzi.TUI.Tests",
        project_root / "test" / "Mogzi.Tests"
    ]
    output_file = project_root / "outputs" / "latest_rqmts_trace_matrix.md"
    html_output_file = project_root / "outputs" / "latest_rqmts_trace_matrix.html"
    
    # Parse requirements
    print("📋 Parsing requirements...")
    requirements = parse_requirements(requirements_file)
    
    if not requirements:
        print("❌ No requirements found. Please check the requirements file.")
        sys.exit(1)
    
    # Scan test files
    print("🔍 Scanning test files...")
    requirement_tests = scan_test_files(test_directories)
    
    # Generate trace matrix (markdown)
    print("📝 Generating markdown trace matrix...")
    generate_trace_matrix(requirements, requirement_tests, output_file)
    
    # Generate HTML trace matrix
    print("🌐 Generating HTML trace matrix...")
    generate_html_trace_matrix(requirements, requirement_tests, html_output_file)
    
    print("🎉 Trace matrix generation complete!")


if __name__ == "__main__":
    main()
