import { Grid, Typography } from '@material-ui/core';
import { HttpRequest } from '@episerver/csr-extension';
import * as React from 'react';
import CircularProgress from '@material-ui/core/CircularProgress';
import * as Yup from 'yup';
import MaskedInput from 'react-maskedinput';
declare let TokenEx: any;

export interface TokenExPayProps {
  onTokenlized(data: any, isValid: boolean): void;
  setTokenizationRef(ref: any): void;
}

class TokenExPay extends React.Component<TokenExPayProps> {
  tokenEx: any;
  _isMounted = false;

  state = {
    tokenExIframeLoaded: false,
    tokenExIframeIsValid: true,
    tokenExIframeLoading: true,
    tokenExError: '',
    expiredDate: '',
    errors: {
      ExpiredDate: '',
    },
    ccNumberError: '',
    cvvError: '',
    focus: false,
  };

  validationSchema = Yup.object({
    ExpiredDate: Yup.string()
      .required('Expire date is required.')
      .matches(/^\d{2}([/])\d{2}$/, { message: 'Expire date is not in correct format.', excludeEmptyString: true }),
  });

  async componentDidMount() {
    this._isMounted = true;
    this.props.setTokenizationRef({ tokenize: this.tokenize, reset: this.reset });
    this.loadTokenExForm();
  }

  loadTokenExForm = async () => {
    const config = await (await HttpRequest.post('tokenEx/config')).data;

    let iframeConfig = {
      tokenExID: config.tokenExID,
      authenticationKey: config.authenticationKey,
      timestamp: config.timestamp,
      tokenScheme: config.tokenScheme,
      origin: window.location.origin,
      cvv: true,
      cvvContainerID: 'card-cvv-element',
      debug: false,
      pci: true,
      placeholder: 'CC number',
      cvvPlaceholder: 'CVV',
      styles: {
        base:
          'padding: 0 8px;border: 1px solid rgba(0, 0, 0, 0.2);margin: 0;width: 100%;font-size: inherit;line-height: 30px;height: 3.7em;box-sizing: border-box;-moz-box-sizing: border-box; border-radius: 5px;',
        focus: 'outline-color: #1456F1;',
        error: 'box-shadow: 0 0 6px 0 rgba(224, 57, 57, 0.5);border: 1px solid rgba(224, 57, 57, 0.5);',
        cvv: {
          base:
            'padding: 0 8px;border: 1px solid rgba(0, 0, 0, 0.2);margin: 0;width: 100%;font-size: inherit;line-height: 30px;height: 3.7em;box-sizing: border-box;-moz-box-sizing: border-box; border-radius: 5px;',
          focus: 'outline-color: #1456F1;',
          error: 'box-shadow: 0 0 6px 0 rgba(224, 57, 57, 0.5);border: 1px solid rgba(224, 57, 57, 0.5);',
        },
      },
    };
    this.tokenEx = new TokenEx.Iframe('card-element', iframeConfig);

    // Add event listeners here
    this.tokenEx.on('load', () => {
      this.onTokenExLoaded();
    });

    this.tokenEx.on('error', (data: any) => {
      this.setState({ tokenExIframeIsValid: true, tokenExIframeLoading: false, tokenExError: data.error });
    });

    this.tokenEx.on('validate', (data: any) => {
      if (data.isValid === false || data.isCvvValid === false) {
        this.setState({ tokenExIframeIsValid: false });
        let ccNumberErrorMessage = '';
        if (data.isValid === false) {
          switch (data.validator) {
            case 'format':
              ccNumberErrorMessage = 'CC number is not in correct format.';
              break;
            case 'required':
              ccNumberErrorMessage = 'CC number is required.';
              break;
            default:
              ccNumberErrorMessage = 'CC number is not valid.';
          }
        }
        this.setState({
          ccNumberError: ccNumberErrorMessage,
        });
        let cvvErrorMessage = '';
        if (data.isCvvValid === false) {
          switch (data.cvvValidator) {
            case 'format':
              cvvErrorMessage = 'CVV is not in correct format.';
              break;
            case 'required':
              cvvErrorMessage = 'CVV is required.';
              break;
            default:
              cvvErrorMessage = 'CVV is not valid.';
          }
        }
        this.setState({
          cvvError: cvvErrorMessage,
        });
      } else {
        this.setState({ tokenExIframeIsValid: true, tokenExError: '' });
      }
    });

    this.tokenEx.on('tokenize', (data: any) => {
      data["expiredDate"] = this.state.expiredDate;
      if (this.props.onTokenlized && this._isMounted) {
        this.props.onTokenlized(data, this.state.tokenExIframeIsValid);
      }
    });

    this.tokenEx.load();
  };

  reset = () => {
    this.tokenEx.reset();
    this.setState({ expiredDate: '', ccNumberError: '', cvvError: '', tokenExError: '' });
  };

  onTokenExLoaded = () => {
    this.setState({ tokenExIframeLoaded: true, tokenExIframeLoading: false });
  };

  tokenize = async () => {
    const isValid= await this.validateForm();
    if (isValid) {
      this.tokenEx.tokenize();
    }
  };

  validateForm = async () => {
    let isValid = true;
    let errors = {};
    try {
      await this.validationSchema.validate(
        {
          ExpiredDate: this.state.expiredDate,
        },
        { abortEarly: false }
      );
    } catch (err) {
      isValid = false;
      err.inner.map((e: any) => {
        errors[e.path] = e.message;
      });
    }
    this.setState({
      errors: errors,
      tokenExIframeIsValid: isValid,
    });
    return isValid;
  };

  changeExpiredDateHandler = async (newValue: string) => {
    this.setState({ expiredDate: newValue });
  };

  changeOutlineColor = async (value: boolean) => {
    this.setState({ focus: value });
  };

  componentWillUnmount() {
    this._isMounted = false;
  }

  render() {
    return (
      <Grid container spacing={0} style={{ marginTop: '5px' }}>
        <Grid
          item
          style={{
            display: this.state.tokenExIframeLoaded === true ? 'block' : 'none',
          }}
        >
          <Grid container>
            <Grid item xs={5}>
              <div id="card-element" style={{ width: '100%', height: '59px' }}></div>
            </Grid>
            <Grid item xs={3}>
              <MaskedInput
                mask="11/11"
                name="expiry"
                value={this.state.expiredDate}
                placeholder="mm/yy"
                maxLength={5}
                onChange={event => this.changeExpiredDateHandler(event.target.value)}
                onFocus={() => this.changeOutlineColor(true)}
                onBlur={() => this.changeOutlineColor(false)}
                style={{
                  lineHeight: '30px',
                  fontSize: 'inherit',
                  display: 'block',
                  padding: '0 5px',
                  width: '80px',
                  margin: 'auto',
                  boxSizing: 'border-box',
                  border: this.state.errors.ExpiredDate ? '1px solid #f01c10' : '1px solid rgba(0, 0, 0, 0.2)',
                  height: '59px',
                  borderRadius: '5px',
                  outlineColor: this.state.focus === true ? 'rgb(20, 86, 241)' : 'rgba(0, 0, 0, 0.2)',
                }}
              />
            </Grid>
            <Grid item xs={4}>
              <div id="card-cvv-element" style={{ width: '100%', height: '59px' }}></div>
            </Grid>
          </Grid>
        </Grid>
        {this.state.tokenExIframeLoading && (
          <Grid item xs={12} style={{ marginTop: '5px' }}>
            <CircularProgress />
          </Grid>
        )}
        <Grid container>
          <Grid item xs={12}>
            {this.state.tokenExError !='' && (
              <Typography variant="caption" color="error">
                {this.state.tokenExError}
              </Typography>
            )}
          </Grid>
          <Grid item xs={5}>
            {this.state.ccNumberError && (
              <Typography variant="caption" color="error">
                {this.state.ccNumberError}
              </Typography>
            )}
          </Grid>
          <Grid item xs={3}>
            {this.state.errors && (
              <Typography variant="caption" color="error">
                {this.state.errors.ExpiredDate}
              </Typography>
            )}
          </Grid>
          <Grid item xs={4}>
            {this.state.cvvError && (
              <Typography variant="caption" color="error">
                {this.state.cvvError}
              </Typography>
            )}
          </Grid>
        </Grid>
      </Grid>
    );
  }
}

export default TokenExPay;
