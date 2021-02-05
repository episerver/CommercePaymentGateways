import * as React from 'react';
import { AppSettingState } from '@episerver/csr-app';
import TokenEx from '../tokenization-services/tokenex';

export const setting = {
  pageConfig: {
    /**
     * Config add payment form
     */
    addPaymentForm: {
      extendUI: {
        /**
         * Put component at the top of page
         */
        topPlaceHolder:  (props) => <TokenEx {...props} />,
      },
    }, 
  }
} as AppSettingState;

