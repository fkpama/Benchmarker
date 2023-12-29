import { expect } from "chai";
import { parseDate } from "../lib/common/utilities";

describe('Utilities', () => {
    describe('Datetime', () => {

        const date = '2023-08-11T19:34:25.6022847Z';

        it('should parse dates', function() {
            let timestamp = parseDate(date);
            expect(timestamp.utc().year()).to.eq(2023);
            expect(timestamp.utc().hour()).to.eq(19);
        })
    });
});
