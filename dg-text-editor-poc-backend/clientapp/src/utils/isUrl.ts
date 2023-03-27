const PROTOCOL_AND_DOMAIN_REGEX = /^(?:\w+:)?\/\/(\S+)$/;

const LOCALHOST_DOMAIN_REGEX = /^localhost[\:?\d]*(?:[^\:?\d]\S*)?$/;
const NON_LOCALHOST_DOMAIN_REGEX = /^[^\s\.]+\.\S{2,}$/;

export const isUrl = (value: string) => {
  if (typeof value !== "string") {
    return false;
  }

  var match = value.match(PROTOCOL_AND_DOMAIN_REGEX);
  if (!match) {
    return false;
  }

  var everythingAfterProtocol = match[1];
  if (!everythingAfterProtocol) {
    return false;
  }

  if (
    LOCALHOST_DOMAIN_REGEX.test(everythingAfterProtocol) ||
    NON_LOCALHOST_DOMAIN_REGEX.test(everythingAfterProtocol)
  ) {
    return true;
  }

  return false;
};
