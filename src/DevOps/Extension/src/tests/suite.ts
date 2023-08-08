import { describe } from 'mocha';
import { SearchPatternCollection, filterPatterns, splitPatterns } from '../tasks/task-utilities';
import { expect } from 'chai';

describe('Utilities:Tasks', () => {
    describe('Tasks', () => {
        describe('Filesystem', () => {


            it('Should be true', function () {
                let result = splitPatterns("hello\nworld\n!my\n");
                expect(result.included.length).to.eq(2);
                expect(result.excluded.length).to.eq(1);
            })

            let patterns: SearchPatternCollection = {
                included: [
                    "**/*.json",
                    "**/*json*/**",
                ],
                excluded: [
                    "!**/notincluded*/**"
                ]
            };

            it('Should filter patterns', function () {

                var paths = [
                    "hello.json",
                    "/some/file/withjson/in/it",
                    "some/notincludedAwesome/File"
                ];
                let result = filterPatterns(paths, patterns);
                expect(result.length).to.eq(2);
            })

        });
    })
})
