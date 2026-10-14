"""Write Unity .meta files for new scripts and folders, so a code-only project keeps stable GUIDs.

usage: python3 -I tools/new_meta.py <path> [<path> ...]

Creates <path>.meta for each path that doesn't have one yet: a folder gets a folder meta, a .cs file a
MonoImporter meta, and anything else (an .asmdef, say) a DefaultImporter meta. Existing .meta files are
left alone.
"""
import os
import sys
import uuid

FOLDER = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

SCRIPT = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

ASMDEF = """fileFormatVersion: 2
guid: {guid}
AssemblyDefinitionImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

OTHER = """fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def main(paths):
    for path in paths:
        path = path.rstrip('/')
        meta = path + '.meta'
        if os.path.exists(meta):
            continue
        if os.path.isdir(path):
            template = FOLDER
        elif path.endswith('.cs'):
            template = SCRIPT
        elif path.endswith('.asmdef'):
            template = ASMDEF
        else:
            template = OTHER
        with open(meta, 'w', encoding='utf-8', newline='\n') as f:
            f.write(template.format(guid=uuid.uuid4().hex))
        print('wrote', meta)


if __name__ == '__main__':
    main(sys.argv[1:])
