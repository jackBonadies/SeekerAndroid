import React from 'react';

import { Message } from 'semantic-ui-react';

const DeprecationWarning = (props) => 
  <Message 
    warning
    icon='warning circle'
    header='Deprecation Notice'
    content={
      <span>This application has been superseded by <a href="https://github.com/slskd/slskd">slskd</a>.</span>
    }
    {...props}
  />

export default DeprecationWarning;