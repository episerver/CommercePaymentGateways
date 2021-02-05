import * as React from 'react';
import ReactDOM from 'react-dom';
import { CSRApp } from '@episerver/csr-app';
import { setting } from './configuration/tokenEx-payment-setting';

import '@episerver/csr-app/bundle.css';

ReactDOM.render(
  <div>
    <CSRApp setting={setting} />
  </div>,
  document.getElementById('root')
);